window.workshopStatusDnd = {
  _outsideHandlers: {},
  _nextContainerId: 1,

  _asElement: function (target) {
    if (!target) {
      return null;
    }

    if (target.nodeType === 1) {
      return target;
    }

    return target.parentElement || null;
  },

  init: function (container, dotnetRef, methodName, rowSelector, handleSelector) {
    if (!container || container.dataset.dndInit === "1") {
      return;
    }

    methodName = methodName || "ReorderStatus";
    rowSelector = rowSelector || ".status-row";
    handleSelector = handleSelector || "";
    container.dataset.dndInit = "1";
    if (!container.dataset.dndContainerId) {
      container.dataset.dndContainerId = "dnd-" + (this._nextContainerId++);
    }
    container._dndPointerDownTarget = null;

    container.addEventListener("pointerdown", function (e) {
      container._dndPointerDownTarget = window.workshopStatusDnd._asElement(e.target);
    }, true);

    container.addEventListener("dragstart", function (e) {
      var target = window.workshopStatusDnd._asElement(e.target);
      if (!target || typeof target.closest !== "function") return;
      var row = target.closest(rowSelector);
      if (!row) return;
      if (handleSelector) {
        var origin = container._dndPointerDownTarget || target;
        var handle = origin && typeof origin.closest === "function"
          ? origin.closest(handleSelector)
          : null;
        if (!handle || !row.contains(handle)) return;
      }
      if (!e.dataTransfer) return;
      e.dataTransfer.effectAllowed = "move";
      e.dataTransfer.setData("text/plain", row.dataset.index);
      e.dataTransfer.setData("application/x-workshop-dnd-container", container.dataset.dndContainerId || "");
      row.classList.add("dragging");
    });

    container.addEventListener("dragend", function (e) {
      var target = window.workshopStatusDnd._asElement(e.target);
      if (!target || typeof target.closest !== "function") return;
      var row = target.closest(rowSelector);
      if (!row) return;
      row.classList.remove("dragging");
      container._dndPointerDownTarget = null;
    });

    container.addEventListener("dragover", function (e) {
      var target = window.workshopStatusDnd._asElement(e.target);
      if (target && typeof target.closest === "function" && target.closest(rowSelector)) {
        e.preventDefault();
      }
    });

    container.addEventListener("drop", function (e) {
      var target = window.workshopStatusDnd._asElement(e.target);
      if (!target || typeof target.closest !== "function") return;
      var row = target.closest(rowSelector);
      if (!row) return;
      if (!e.dataTransfer) return;
      e.preventDefault();
      var sourceContainerId = e.dataTransfer.getData("application/x-workshop-dnd-container");
      if (!sourceContainerId || sourceContainerId !== (container.dataset.dndContainerId || "")) {
        return;
      }
      var from = parseInt(e.dataTransfer.getData("text/plain"), 10);
      var to = parseInt(row.dataset.index, 10);
      if (Number.isNaN(from) || Number.isNaN(to)) return;
      if (dotnetRef && methodName) {
        var key = container.dataset && container.dataset.dndKey ? container.dataset.dndKey : "";
        if (key) {
          dotnetRef.invokeMethodAsync(methodName, key, from, to);
        } else {
          dotnetRef.invokeMethodAsync(methodName, from, to);
        }
      }
      container._dndPointerDownTarget = null;
    });
  },

  initSelector: function (selector, dotnetRef, methodName, rowSelector, handleSelector) {
    if (!selector || !dotnetRef || !methodName) {
      return;
    }

    var containers = document.querySelectorAll(selector);
    if (!containers || containers.length === 0) {
      return;
    }

    for (var i = 0; i < containers.length; i++) {
      this.init(containers[i], dotnetRef, methodName, rowSelector, handleSelector);
    }
  },

  bindOutside: function (key, dotnetRef, methodName, inputSelector) {
    if (!key || !dotnetRef || !methodName) {
      return;
    }

    this.unbindOutside(key);

    var handler = function (e) {
      var target = window.workshopStatusDnd._asElement(e.target);
      if (inputSelector && target && typeof target.closest === "function" && target.closest(inputSelector)) {
        return;
      }

      dotnetRef.invokeMethodAsync(methodName);
    };

    document.addEventListener("pointerdown", handler, true);
    this._outsideHandlers[key] = handler;
  },

  unbindOutside: function (key) {
    if (!key) {
      return;
    }

    var existing = this._outsideHandlers[key];
    if (!existing) {
      return;
    }

    document.removeEventListener("pointerdown", existing, true);
    delete this._outsideHandlers[key];
  },

  focusElement: function (element, scrollToElement, scrollBlock, bottomPadding) {
    if (!element || typeof element.focus !== "function") {
      return;
    }

    setTimeout(function () {
      if (scrollToElement && typeof element.scrollIntoView === "function") {
        element.scrollIntoView({
          behavior: "smooth",
          block: scrollBlock || "center",
          inline: "nearest"
        });

        var pad = Number(bottomPadding || 0);
        if (!Number.isNaN(pad) && pad > 0) {
          setTimeout(function () {
            var rect = element.getBoundingClientRect();
            var maxBottom = window.innerHeight - pad;
            if (rect.bottom > maxBottom) {
              window.scrollBy({
                top: rect.bottom - maxBottom,
                behavior: "smooth"
              });
            }
          }, 120);
        }
      }
      element.focus();
      if (typeof element.select === "function") {
        element.select();
      }
    }, 0);
  }
};
