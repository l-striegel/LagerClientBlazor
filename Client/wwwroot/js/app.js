window.focusElement = function (element) {
    if (element) {
        element.focus();
    }
};
window.downloadFile = function(filename, content) {
    try {
        var blob = new Blob([content], { type: 'application/json' });
        var url = window.URL.createObjectURL(blob);
        
        var link = document.createElement('a');
        link.href = url;
        link.download = filename;
        
        // An Body anhängen, klicken und entfernen
        document.body.appendChild(link);
        link.click();
        
        // Aufräumen
        setTimeout(function() {
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
        }, 100);
    } catch (error) {
        console.error("Download-Fehler:", error);
    }
};
window.importFile = function() {
    return new Promise((resolve, reject) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json';
        
        input.onchange = e => {
            const file = e.target.files[0];
            if (!file) {
                resolve(null);
                return;
            }
            
            const reader = new FileReader();
            reader.readAsText(file, 'UTF-8');
            
            reader.onload = readerEvent => {
                const content = readerEvent.target.result;
                resolve(content);
            };
            
            reader.onerror = error => {
                reject(error);
                console.error("Fehler beim Lesen der Datei:", error);
            };
        };
        
        input.click();
    });
};
// Funktion, um das Fokussieren von Zellen zu verhindern
window.preventCellFocus = function() {
    // Entfernt den Fokus von allen Elementen
    if (document.activeElement) {
        document.activeElement.blur();
    }
};