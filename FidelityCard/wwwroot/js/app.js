window.manifestInterop = {
    getManifest: async function () {
        const response = await fetch('/manifest.webmanifest?k=3');        
        if (!response.ok) {
            throw new Error('Manifest not found');
        }
        return await response.json();
    },
    initDatePicker: function (elementId) {
        flatpickr("#" + elementId, {
            locale: "it",
            dateFormat: "d/m/Y",
            maxDate: "today",
            disableMobile: "true",
            allowInput: true
        });
    }
};