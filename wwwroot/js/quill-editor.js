window.quillEditors = {};

window.createQuill = (editorId, initialContent) => {
    if (typeof window.Quill === "undefined") {
        console.error("Quill library not loaded. Check /js/quill.min.js path.");
        return;
    }

    const container = document.getElementById(editorId);
    if (!container) return;

    // Reset container (important when reopening the form)
    container.innerHTML = "";

    const quill = new Quill(container, {
        theme: "snow",
        modules: {
            toolbar: [
                ["bold", "italic", "underline"],
                [{ list: "ordered" }, { list: "bullet" }],
                [{ header: [1, 2, 3, false] }],
                ["link"],
                ["clean"]
            ]
        }
    });

    if (initialContent) {
        quill.clipboard.dangerouslyPasteHTML(initialContent);
    }

    window.quillEditors[editorId] = quill;
};

window.getQuillHtml = (editorId) => {
    const q = window.quillEditors[editorId];
    return q ? q.root.innerHTML : "";
};
