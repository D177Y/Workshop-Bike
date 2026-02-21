window.workshopSmartSidebar = (function () {
  var state = null;

  function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
  }

  function isDesktop() {
    return window.matchMedia("(min-width: 600px)").matches;
  }

  function resolveElements() {
    var shell = document.getElementById("app-shell");
    if (!shell) return null;

    var toolbar = shell.querySelector(".app-toolbar");
    var sidebar = shell.querySelector(".app-sidebar");
    var menu = sidebar ? sidebar.querySelector(".app-menu") : null;
    if (!sidebar || !menu) return null;

    return {
      shell: shell,
      toolbar: toolbar,
      sidebar: sidebar,
      menu: menu
    };
  }

  function getHeaderOffset(toolbar) {
    if (!toolbar) return 64;
    var rect = toolbar.getBoundingClientRect();
    var height = Math.round(rect.height || 0);
    return height > 0 ? height : 64;
  }

  function applyMode(mode, headerOffset, offsetPx) {
    if (!state || !state.sidebar || !state.menu) return;

    state.sidebar.classList.add("smart-sticky-active");
    state.sidebar.classList.toggle("smart-sticky-short", mode === "short");
    state.sidebar.classList.toggle("smart-sticky-tall", mode === "tall");

    state.menu.style.setProperty("--sidebar-smart-top", headerOffset + "px");
    state.menu.style.setProperty("--sidebar-smart-offset", offsetPx + "px");
  }

  function resetMode() {
    if (!state || !state.sidebar || !state.menu) return;

    state.sidebar.classList.remove("smart-sticky-active", "smart-sticky-short", "smart-sticky-tall");
    state.menu.style.removeProperty("--sidebar-smart-top");
    state.menu.style.removeProperty("--sidebar-smart-offset");
  }

  function recalc(forceResetOffset) {
    if (!state) return;

    var desktop = isDesktop();
    var sidebarVisible = state.sidebar && state.sidebar.offsetParent !== null;
    if (!desktop || !sidebarVisible) {
      state.offset = 0;
      state.maxOffset = 0;
      state.lastScrollY = window.scrollY || window.pageYOffset || 0;
      resetMode();
      return;
    }

    var headerOffset = getHeaderOffset(state.toolbar);
    var availableHeight = Math.max(0, window.innerHeight - headerOffset);
    var menuHeight = state.menu.scrollHeight;
    var maxOffset = Math.max(0, menuHeight - availableHeight);

    if (forceResetOffset)
      state.offset = 0;

    state.maxOffset = maxOffset;
    state.offset = clamp(state.offset, 0, maxOffset);

    if (maxOffset <= 0) {
      applyMode("short", headerOffset, 0);
      return;
    }

    applyMode("tall", headerOffset, state.offset);
  }

  function handleScroll() {
    if (!state) return;

    var currentY = window.scrollY || window.pageYOffset || 0;
    var delta = currentY - state.lastScrollY;
    state.lastScrollY = currentY;

    recalc(false);

    if (state.maxOffset <= 0) {
      state.offset = 0;
      return;
    }

    if (currentY <= 0) {
      state.offset = 0;
    } else if (delta !== 0) {
      state.offset = clamp(state.offset + delta, 0, state.maxOffset);
    }

    var headerOffset = getHeaderOffset(state.toolbar);
    applyMode("tall", headerOffset, state.offset);
  }

  function handleResize() {
    recalc(false);
  }

  function observeMenu() {
    if (!state || !state.menu || typeof ResizeObserver === "undefined") return;

    state.resizeObserver = new ResizeObserver(function () {
      recalc(false);
    });
    state.resizeObserver.observe(state.menu);
  }

  function disposeObservers() {
    if (!state) return;

    if (state.resizeObserver) {
      state.resizeObserver.disconnect();
      state.resizeObserver = null;
    }
  }

  function init() {
    var elements = resolveElements();
    if (!elements) return;

    if (state) {
      state.shell = elements.shell;
      state.toolbar = elements.toolbar;
      state.sidebar = elements.sidebar;
      state.menu = elements.menu;
      refresh();
      return;
    }

    state = {
      shell: elements.shell,
      toolbar: elements.toolbar,
      sidebar: elements.sidebar,
      menu: elements.menu,
      offset: 0,
      maxOffset: 0,
      lastScrollY: window.scrollY || window.pageYOffset || 0,
      resizeObserver: null
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    window.addEventListener("resize", handleResize, { passive: true });
    observeMenu();
    recalc(true);
  }

  function refresh() {
    if (!state) {
      init();
      return;
    }

    var elements = resolveElements();
    if (!elements) {
      resetMode();
      return;
    }

    var menuChanged = state.menu !== elements.menu;
    state.shell = elements.shell;
    state.toolbar = elements.toolbar;
    state.sidebar = elements.sidebar;
    state.menu = elements.menu;

    if (menuChanged) {
      disposeObservers();
      observeMenu();
      state.offset = 0;
    }

    recalc(false);
  }

  function destroy() {
    if (!state) return;

    window.removeEventListener("scroll", handleScroll);
    window.removeEventListener("resize", handleResize);
    disposeObservers();
    resetMode();
    state = null;
  }

  return {
    init: init,
    refresh: refresh,
    destroy: destroy
  };
})();
