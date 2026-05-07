window.scrollToBottom = (el) => {
    if (el) el.scrollTop = el.scrollHeight;
};

window.isScrolledToBottom = (el) => {
    if (!el) return true;
    return el.scrollHeight - el.scrollTop - el.clientHeight < 6;
};

window.syncArticlePadding = (panelEl) => {
    const article = document.querySelector('main > article.content');
    if (article && panelEl) article.style.paddingBottom = panelEl.offsetHeight + 'px';
    else if (article) article.style.paddingBottom = '';
};

window.initOutputPanelResize = (panelEl, handleEl) => {
    if (!panelEl || !handleEl) return;
    const syncPadding = () => {
        const article = document.querySelector('main > article.content');
        if (article) article.style.paddingBottom = panelEl.offsetHeight + 'px';
    };
    syncPadding();
    handleEl.addEventListener('mousedown', (e) => {
        const startY = e.clientY;
        const startHeight = panelEl.offsetHeight;
        e.preventDefault();
        document.body.style.userSelect = 'none';
        const onMove = (mv) => {
            const newH = Math.max(60, Math.min(window.innerHeight * 0.7, startHeight + (startY - mv.clientY)));
            panelEl.style.height = newH + 'px';
            syncPadding();
        };
        const onUp = () => {
            document.body.style.userSelect = '';
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
        };
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    });
};

window.downloadFileFromStream = async (fileName, streamRef) => {
            const arrayBuffer = await streamRef.arrayBuffer();
            const blob = new Blob([arrayBuffer]);
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = fileName;
            a.click();
            URL.revokeObjectURL(url);
        }

window.initializeFileDropZone = async (dropZoneElement, inputFile) => {
    // Add a class when the user drags a file over the drop zone
    function onDragHover(e) {
        e.preventDefault();
        dropZoneElement.classList.add("hover");
    }

    function onDragLeave(e) {
        e.preventDefault();
        dropZoneElement.classList.remove("hover");
    }

    // Handle the paste and drop events
    function onDrop(e) {
        e.preventDefault();
        dropZoneElement.classList.remove("hover");

        // Set the files property of the input element and raise the change event
        inputFile.files = e.dataTransfer.files;
        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }

    function onPaste(e) {
        // Set the files property of the input element and raise the change event
        inputFile.files = e.clipboardData.files;
        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }

    // Register all events
    dropZoneElement.addEventListener("dragenter", onDragHover);
    dropZoneElement.addEventListener("dragover", onDragHover);
    dropZoneElement.addEventListener("dragleave", onDragLeave);
    dropZoneElement.addEventListener("drop", onDrop);
    dropZoneElement.addEventListener('paste', onPaste);

    // The returned object allows to unregister the events when the Blazor component is destroyed
    return {
        dispose: () => {
            dropZoneElement.removeEventListener('dragenter', onDragHover);
            dropZoneElement.removeEventListener('dragover', onDragHover);
            dropZoneElement.removeEventListener('dragleave', onDragLeave);
            dropZoneElement.removeEventListener("drop", onDrop);
            dropZoneElement.removeEventListener('paste', onPaste);
        }
    }
}    