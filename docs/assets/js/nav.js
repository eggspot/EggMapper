/* EggMapper docs — navigation + in-page TOC */
(function () {

  /* ── Mobile sidebar toggle ─────────────────────── */
  var toggle  = document.getElementById('menuToggle');
  var sidebar = document.getElementById('sidebar');
  var overlay = document.getElementById('overlay');

  function openSidebar()  { sidebar.classList.add('open');    overlay.classList.add('active'); }
  function closeSidebar() { sidebar.classList.remove('open'); overlay.classList.remove('active'); }

  if (toggle)  toggle.addEventListener('click', function () {
    sidebar.classList.contains('open') ? closeSidebar() : openSidebar();
  });
  if (overlay) overlay.addEventListener('click', closeSidebar);

  /* ── Active nav link ───────────────────────────── */
  var page = location.pathname.split('/').pop() || 'index.html';
  if (page === '') page = 'index.html';
  var activeLink = null;
  document.querySelectorAll('.nav-link').forEach(function (a) {
    if (a.getAttribute('href') === page) {
      a.classList.add('active');
      activeLink = a;
    }
  });

  /* ── Copy buttons ──────────────────────────────── */
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

  /* ── In-page TOC (sidebar sub-items + right panel) */
  var wrapper = document.querySelector('.content-wrapper');
  if (!wrapper) return;

  var headings = Array.from(wrapper.querySelectorAll('h2'));
  if (headings.length < 2) return;

  /* Assign stable IDs */
  headings.forEach(function (h) {
    if (!h.id) {
      h.id = h.textContent.trim()
        .toLowerCase()
        .replace(/[^a-z0-9\s]/g, '')
        .trim()
        .replace(/\s+/g, '-');
    }
  });

  /* Clean display label: strip " — subtitle" and trailing "()" */
  function label(h) {
    return h.textContent.replace(/\s+\u2014.*/, '').replace(/\(\)\s*$/, '').trim();
  }

  /* 1) Sidebar sub-items injected after the active link */
  var subNav = null;
  if (activeLink) {
    subNav = document.createElement('div');
    subNav.className = 'nav-page-toc';
    headings.forEach(function (h) {
      var a = document.createElement('a');
      a.href = '#' + h.id;
      a.className = 'nav-sub-link';
      a.textContent = label(h);
      subNav.appendChild(a);
    });
    activeLink.parentNode.insertBefore(subNav, activeLink.nextSibling);
  }

  /* 2) Right-side floating "On this page" panel */
  var panel = document.createElement('aside');
  panel.className = 'toc-panel';
  var panelLabel = document.createElement('div');
  panelLabel.className = 'toc-panel-label';
  panelLabel.textContent = 'On this page';
  panel.appendChild(panelLabel);
  headings.forEach(function (h) {
    var a = document.createElement('a');
    a.href = '#' + h.id;
    a.className = 'toc-panel-link';
    a.textContent = label(h);
    panel.appendChild(a);
  });
  document.body.appendChild(panel);

  /* 3) Active-section highlight on scroll */
  var panelLinks = panel.querySelectorAll('.toc-panel-link');
  var subLinks   = subNav ? subNav.querySelectorAll('.nav-sub-link') : [];

  function setActive(id) {
    panelLinks.forEach(function (l) {
      l.classList.toggle('toc-active', l.getAttribute('href') === '#' + id);
    });
    subLinks.forEach(function (l) {
      l.classList.toggle('toc-active', l.getAttribute('href') === '#' + id);
    });
  }

  /* Seed first heading as active */
  if (headings[0]) setActive(headings[0].id);

  if ('IntersectionObserver' in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) setActive(e.target.id);
      });
    }, { rootMargin: '-5% 0px -82% 0px' });
    headings.forEach(function (h) { io.observe(h); });
  }

})();
