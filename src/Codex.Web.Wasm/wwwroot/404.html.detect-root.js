const scriptUrl = document.currentScript ? document.currentScript.src : undefined;
console.log('Current script URL:', scriptUrl);

window.rootUrl = scriptUrl ? scriptUrl.substring(0, scriptUrl.lastIndexOf('/')) + '/' : undefined;
console.log('Detected root URL:', window.rootUrl);