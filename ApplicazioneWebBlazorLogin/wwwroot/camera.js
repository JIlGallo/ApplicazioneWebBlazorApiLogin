window.initializeCamera = () => {
    const video = document.getElementById('video');
    const canvas = document.getElementById('canvas');
    const context = canvas.getContext('2d');

    navigator.mediaDevices.getUserMedia({ video: true, audio: false })
        .then((stream) => {
            video.srcObject = stream;
            video.play();
        })
        .catch((err) => {
            console.error('Errore nell\'accesso alla fotocamera:', err);
        });

    window.takePhoto = () => {
        context.drawImage(video, 0, 0, 640, 480);
    }

    window.getCanvasData = () => {
        return canvas.toDataURL();
    }
};