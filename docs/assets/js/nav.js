/* EggMapper site nav */
(function () {
  var toggle   = document.getElementById('menuToggle');
  var sidebar  = document.getElementById('sidebar');
  var overlay  = document.getElementById('overlay');

  function openSidebar()  { sidebar.classList.add('open');    overlay.classList.add('active'); }
  function closeSidebar() { sidebar.classList.remove('open'); overlay.classList.remove('active'); }

  if (toggle)  toggle.addEventListener('click', function () { sidebar.classList.contains('open') ? closeSidebar() : openSidebar(); });
  if (overlay) overlay.addEventListener('click', closeSidebar);

  /* Highlight active nav link */
  var page = location.pathname.split('/').pop() || 'index.html';
  if (page === '') page = 'index.html';
  document.querySelectorAll('.nav-link').forEach(function (a) {
    if (a.getAttribute('href') === page) a.classList.add('active');
  });

  /* Copy buttons */
  document.querySelectorAll('.copy-btn').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var cmd = btn.previousElementSibling;
      if (!cmd) return;
      navigator.clipboard.writeText(cmd.textContent.trim()).then(function () {
        btn.textContent = 'Copied!';
        setTimeout(function () { btn.textContent = 'Copy'; }, 1600);
      });
    });
  });
})();
