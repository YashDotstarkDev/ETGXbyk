(function (window) {
    'use strict';

    const COOKIE_NAME = 'etgUtm';
    const COOKIE_EXPIRATION_DAYS = 100;

    const UTM_PARAMETERS = [
        'utm_source',
        'utm_medium',
        'utm_campaign',
        'utm_content',
        'utm_term'
    ];

    const UtmProcessor = {

        readCookie: function (name) {
            const cookies = document.cookie ? document.cookie.split(';') : [];

            for (let i = 0; i < cookies.length; i++) {
                const cookie = cookies[i].trim();

                if (cookie.indexOf(name + '=') === 0) {
                    return decodeURIComponent(
                        cookie.substring(name.length + 1)
                    );
                }
            }

            return null;
        },

        setCookie: function (name, value, days) {
            const date = new Date();

            date.setTime(
                date.getTime() + (days * 24 * 60 * 60 * 1000)
            );

            const expires = 'expires=' + date.toUTCString();

            document.cookie =
                name + '=' +
                encodeURIComponent(value) +
                ';path=/;' +
                expires +
                ';SameSite=Lax';
        },

        getUrlParams: function () {
            const params = {};
            const searchParams = new URLSearchParams(
                window.location.search
            );

            searchParams.forEach(function (value, key) {
                if (!key) {
                    return;
                }

                if (typeof params[key] === 'undefined') {
                    params[key] = value;
                }
                else if (Array.isArray(params[key])) {
                    params[key].push(value);
                }
                else {
                    params[key] = [
                        params[key],
                        value
                    ];
                }
            });

            return params;
        },

        getUtmParams: function (params) {
            const utm = {};

            UTM_PARAMETERS.forEach(function (parameter) {
                if (
                    Object.prototype.hasOwnProperty.call(
                        params,
                        parameter
                    )
                ) {
                    utm[parameter] = params[parameter];
                }
            });

            return utm;
        },

        hasUtmParams: function (utm) {
            return Object.keys(utm).length > 0;
        },

        areEqual: function (first, second) {
            return UTM_PARAMETERS.every(function (parameter) {
                return (first[parameter] || '') ===
                       (second[parameter] || '');
            });
        },

        init: function () {
            const urlParams = this.getUrlParams();
            const currentUtm = this.getUtmParams(urlParams);

            // Nothing to track on this URL.
            if (!this.hasUtmParams(currentUtm)) {
                return;
            }

            const existingCookie = this.readCookie(COOKIE_NAME);

            // No existing cookie.
            if (!existingCookie) {
                this.setCookie(
                    COOKIE_NAME,
                    JSON.stringify(currentUtm),
                    COOKIE_EXPIRATION_DAYS
                );

                return;
            }

            let storedUtm;

            try {
                storedUtm = JSON.parse(existingCookie);
            }
            catch (error) {
                // Invalid cookie - recreate it.
                storedUtm = null;
            }

            if (!storedUtm || !this.areEqual(storedUtm, currentUtm)) {
                this.setCookie(
                    COOKIE_NAME,
                    JSON.stringify(currentUtm),
                    COOKIE_EXPIRATION_DAYS
                );
            }
        }
    };

    // Expose globally.
    if (typeof window.UtmProcessor === 'undefined') {
        window.UtmProcessor = UtmProcessor;
    }

    // Initialize UTM tracking.
    window.UtmProcessor.init();

})(window);
