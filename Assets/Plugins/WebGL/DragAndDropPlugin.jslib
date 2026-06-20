mergeInto(LibraryManager.library, {
    InitDragAndDrop: function () {
        var canvas = document.getElementById("unity-canvas") || document.body;
        
        canvas.addEventListener('dragover', function(e) {
            e.preventDefault();
        }, false);

        canvas.addEventListener('drop', function(e) {
            e.preventDefault();
            var file = e.dataTransfer.files[0];
            if (!file) return;
            
            var reader = new FileReader();
            reader.onload = function(event) {
                var dataStr = event.target.result;
                if (typeof myUnityInstance !== 'undefined') {
                    myUnityInstance.SendMessage('ModelLoader', 'CarregarModelFromWebBase64', dataStr);
                } else if (typeof unityInstance !== 'undefined') {
                    unityInstance.SendMessage('ModelLoader', 'CarregarModelFromWebBase64', dataStr);
                } else {
                    console.error("No s'ha trobat la instància de Unity.");
                }
            };
            reader.readAsDataURL(file);
        }, false);
    }
});
