window.onload = function () {
    // ... existing Swagger UI customization code ...

    // Ensure Content-Type is set to application/x-www-form-urlencoded for token requests
    if (window.ui) {
        window.ui.getConfigs().requestInterceptor = (request) => {
            if (request.url.endsWith('/connect/token')) {
                request.headers['Content-Type'] = 'application/x-www-form-urlencoded';
            }
            return request;
        };
    }
};