window.workshopStatusDnd = {
  init: function (container, dotnetRef, methodName, rowSelector) {
    if (!container || container.dataset.dndInit === "1") {
      return;
    }

    methodName = methodName || "ReorderStatus";
    rowSelector = rowSelector || ".status-row";
    container.dataset.dndInit = "1";

    container.addEventListener("dragstart", function (e) {
      var row = e.target.closest(rowSelector);
      if (!row) return;
      e.dataTransfer.effectAllowed = "move";
      e.dataTransfer.setData("text/plain", row.dataset.index);
      row.classList.add("dragging");
    });

    container.addEventListener("dragend", function (e) {
      var row = e.target.closest(rowSelector);
      if (!row) return;
      row.classList.remove("dragging");
    });

    container.addEventListener("dragover", function (e) {
      if (e.target.closest(rowSelector)) {
        e.preventDefault();
      }
    });

    container.addEventListener("drop", function (e) {
      var row = e.target.closest(rowSelector);
      if (!row) return;
      e.preventDefault();
      var from = parseInt(e.dataTransfer.getData("text/plain"), 10);
      var to = parseInt(row.dataset.index, 10);
      if (Number.isNaN(from) || Number.isNaN(to)) return;
      if (dotnetRef && methodName) {
        dotnetRef.invokeMethodAsync(methodName, from, to);
      }
    });
  }
};
