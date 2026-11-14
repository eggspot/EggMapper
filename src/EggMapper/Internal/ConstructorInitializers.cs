using System.Reflection;

namespace EggMapper.Internal;

internal static class ConstructorInitializers
{
    private const int MaxConstructorDepth = 4;
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private enum StackSlot
    {
        This,
        Value,
        FreshInstance
    }

    private sealed class ConstructorBody
    {
        public readonly Dictionary<FieldInfo, ConstructorInfo> FreshFieldStores = new();
        public readonly Dictionary<FieldInfo, int> FieldStoreCounts = new();
    }

    internal static IReadOnlyDictionary<PropertyInfo, Type> GetPropertiesInitializedWithNewInstance(Type type)
    {
        try
        {
            return FindPropertiesInitializedWithNewInstance(type);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            return NoProperties;
        }
    }

    private static readonly IReadOnlyDictionary<PropertyInfo, Type> NoProperties = new Dictionary<PropertyInfo, Type>();

    private static IReadOnlyDictionary<PropertyInfo, Type> FindPropertiesInitializedWithNewInstance(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            return NoProperties;
        if (!AllPropertiesAreTrivialAutoProperties(type))
            return NoProperties;

        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor == null)
            return NoProperties;

        var body = ReadConstructor(ctor, isDestinationConstructor: true);
        if (body == null || !BaseConstructorsAreSimple(type.BaseType, 0))
            return NoProperties;

        var result = new Dictionary<PropertyInfo, Type>();
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.DeclaringType != type || property.PropertyType.IsValueType) continue;
            var field = FindBackingField(type, property);
            if (field == null) continue;
            if (!body.FreshFieldStores.TryGetValue(field, out var freshCtor)) continue;
            if (body.FieldStoreCounts[field] != 1) continue;
            if (!IsReusableInstanceType(freshCtor.DeclaringType!, property.PropertyType)) continue;
            result[property] = freshCtor.DeclaringType!;
        }
        return result.Count == 0 ? NoProperties : result;
    }

    private static bool IsReusableInstanceType(Type instanceType, Type propertyType)
    {
        if (instanceType.IsGenericType && instanceType.GetGenericTypeDefinition() == typeof(List<>))
            return propertyType.IsAssignableFrom(instanceType);
        return instanceType == propertyType && IsSideEffectFreeToConstruct(instanceType, 0);
    }

    private static bool IsSideEffectFreeToConstruct(Type type, int depth)
    {
        if (depth > MaxConstructorDepth) return false;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return true;
        if (!type.IsClass || type.IsAbstract || HasFinalizer(type)) return false;
        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor == null) return false;
        var body = ReadConstructor(ctor, isDestinationConstructor: false, depth);
        return body != null && BaseConstructorsAreSimple(type.BaseType, depth);
    }

    private static bool BaseConstructorsAreSimple(Type? baseType, int depth)
    {
        if (baseType == null || baseType == typeof(object)) return true;
        if (HasFinalizer(baseType)) return false;
        var ctor = baseType.GetConstructor(InstanceMembers, null, Type.EmptyTypes, null);
        if (ctor == null) return false;
        return ReadConstructor(ctor, isDestinationConstructor: false, depth) != null
            && BaseConstructorsAreSimple(baseType.BaseType, depth);
    }

    private static bool HasFinalizer(Type type)
    {
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            if (current.GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null, Type.EmptyTypes, null) != null)
                return true;
        }
        return false;
    }

    private static bool AllPropertiesAreTrivialAutoProperties(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0) return false;
            var declaringType = property.DeclaringType!;
            var field = FindBackingField(declaringType, property);
            if (field == null) return false;
            var getter = property.GetGetMethod();
            if (getter != null && !IsTrivialGetter(getter, field)) return false;
            var setter = property.GetSetMethod();
            if (setter != null && !IsTrivialSetter(setter, field)) return false;
        }
        return true;
    }

    private static FieldInfo? FindBackingField(Type declaringType, PropertyInfo property)
        => declaringType.GetField("<" + property.Name + ">k__BackingField", InstanceMembers | BindingFlags.DeclaredOnly);

    private static bool IsTrivialGetter(MethodInfo getter, FieldInfo field)
    {
        var il = getter.GetMethodBody()?.GetILAsByteArray();
        return il != null && il.Length == 7 && il[0] == 0x02 && il[1] == 0x7B && il[6] == 0x2A
            && TokenIsField(getter, il, 2, field);
    }

    private static bool IsTrivialSetter(MethodInfo setter, FieldInfo field)
    {
        var il = setter.GetMethodBody()?.GetILAsByteArray();
        return il != null && il.Length == 8 && il[0] == 0x02 && il[1] == 0x03 && il[2] == 0x7D && il[7] == 0x2A
            && TokenIsField(setter, il, 3, field);
    }

    private static bool TokenIsField(MethodBase method, byte[] il, int offset, FieldInfo field)
    {
        var resolved = ResolveField(method, BitConverter.ToInt32(il, offset));
        return resolved != null && SameField(resolved, field);
    }

    private static bool SameField(FieldInfo a, FieldInfo b)
        => a.MetadataToken == b.MetadataToken && a.Module == b.Module && a.DeclaringType == b.DeclaringType;

    private static FieldInfo? ResolveField(MethodBase method, int token)
    {
        var declaringType = method.DeclaringType;
        var typeArguments = declaringType != null && declaringType.IsGenericType ? declaringType.GetGenericArguments() : null;
        var field = method.Module.ResolveField(token, typeArguments, null);
        if (field == null || declaringType == null || !declaringType.IsGenericType || field.DeclaringType == null)
            return field;
        if (!field.DeclaringType.IsGenericTypeDefinition) return field;
        return declaringType.GetField(field.Name, InstanceMembers | BindingFlags.Static | BindingFlags.DeclaredOnly);
    }

    private static MethodBase? ResolveMethod(MethodBase method, int token)
    {
        var declaringType = method.DeclaringType;
        var typeArguments = declaringType != null && declaringType.IsGenericType ? declaringType.GetGenericArguments() : null;
        return method.Module.ResolveMethod(token, typeArguments, null);
    }

    private static ConstructorBody? ReadConstructor(ConstructorInfo ctor, bool isDestinationConstructor, int depth = 0)
    {
        var methodBody = ctor.GetMethodBody();
        var il = methodBody?.GetILAsByteArray();
        if (methodBody == null || il == null || methodBody.ExceptionHandlingClauses.Count > 0)
            return null;

        var result = new ConstructorBody();
        var stack = new List<StackSlot>();
        var freshConstructors = new Stack<ConstructorInfo>();
        var position = 0;

        while (position < il.Length)
        {
            var opcode = il[position++];
            switch (opcode)
            {
                case 0x00:
                    break;
                case 0x02:
                    stack.Add(StackSlot.This);
                    break;
                case 0x14:
                case >= 0x15 and <= 0x1E:
                    stack.Add(StackSlot.Value);
                    break;
                case 0x1F:
                    position += 1;
                    stack.Add(StackSlot.Value);
                    break;
                case 0x20:
                case 0x22:
                    position += 4;
                    stack.Add(StackSlot.Value);
                    break;
                case 0x21:
                case 0x23:
                    position += 8;
                    stack.Add(StackSlot.Value);
                    break;
                case 0x72:
                    position += 4;
                    stack.Add(StackSlot.Value);
                    break;
                case 0x7E:
                {
                    var staticField = ResolveField(ctor, BitConverter.ToInt32(il, position));
                    position += 4;
                    if (staticField == null || !staticField.IsStatic) return null;
                    if (!isDestinationConstructor && !staticField.IsInitOnly) return null;
                    stack.Add(StackSlot.Value);
                    break;
                }
                case 0x25:
                    if (stack.Count == 0 || stack[stack.Count - 1] != StackSlot.Value) return null;
                    stack.Add(StackSlot.Value);
                    break;
                case >= 0x67 and <= 0x6E:
                case 0x76:
                case >= 0xD1 and <= 0xD3:
                case 0xE0:
                    if (!PopValue(stack)) return null;
                    stack.Add(StackSlot.Value);
                    break;
                case 0x73:
                {
                    var target = ResolveMethod(ctor, BitConverter.ToInt32(il, position)) as ConstructorInfo;
                    position += 4;
                    if (target == null) return null;
                    var parameterCount = target.GetParameters().Length;
                    for (var i = 0; i < parameterCount; i++)
                        if (!PopValue(stack)) return null;
                    var targetType = target.DeclaringType!;
                    if (parameterCount == 0 && !targetType.IsValueType && IsSideEffectFreeToConstruct(targetType, depth + 1))
                    {
                        stack.Add(StackSlot.FreshInstance);
                        freshConstructors.Push(target);
                    }
                    else if (isDestinationConstructor)
                    {
                        stack.Add(StackSlot.Value);
                    }
                    else
                    {
                        return null;
                    }
                    break;
                }
                case 0x7D:
                {
                    var field = ResolveField(ctor, BitConverter.ToInt32(il, position));
                    position += 4;
                    if (field == null || stack.Count < 2) return null;
                    var value = stack[stack.Count - 1];
                    var receiver = stack[stack.Count - 2];
                    stack.RemoveRange(stack.Count - 2, 2);
                    if (receiver != StackSlot.This || value == StackSlot.This) return null;
                    result.FieldStoreCounts[field] = result.FieldStoreCounts.TryGetValue(field, out var count) ? count + 1 : 1;
                    if (value == StackSlot.FreshInstance)
                        result.FreshFieldStores[field] = freshConstructors.Pop();
                    break;
                }
                case 0x28:
                {
                    var target = ResolveMethod(ctor, BitConverter.ToInt32(il, position));
                    position += 4;
                    if (target == null) return null;
                    if (target is ConstructorInfo baseCtor)
                    {
                        if (baseCtor.DeclaringType != ctor.DeclaringType!.BaseType || baseCtor.GetParameters().Length != 0)
                            return null;
                        if (stack.Count != 1 || stack[0] != StackSlot.This) return null;
                        stack.Clear();
                        break;
                    }
                    if (!isDestinationConstructor || !target.IsStatic) return null;
                    var parameterCount = target.GetParameters().Length;
                    for (var i = 0; i < parameterCount; i++)
                        if (!PopValue(stack)) return null;
                    if (target is MethodInfo method && method.ReturnType != typeof(void))
                        stack.Add(StackSlot.Value);
                    break;
                }
                case 0x2A:
                    return stack.Count == 0 && position == il.Length ? result : null;
                default:
                    return null;
            }
        }
        return null;
    }

    private static bool PopValue(List<StackSlot> stack)
    {
        if (stack.Count == 0 || stack[stack.Count - 1] != StackSlot.Value) return false;
        stack.RemoveAt(stack.Count - 1);
        return true;
    }
}
