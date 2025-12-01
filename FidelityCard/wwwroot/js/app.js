window.manifestInterop = {
    getManifest: async function () {
        const response = await fetch('/manifest.webmanifest?k=3');        
        if (!response.ok) {
            throw new Error('Manifest not found');
        }
        return await response.json();
    }
};