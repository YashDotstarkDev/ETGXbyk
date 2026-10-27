(function (window, document) {
    "use strict";

    const COOKIE_NAME = "etgUtm";
    const COOKIE_DAYS = 100;

    const UTM_PARAMETERS = [
        "utm_source",
        "utm_medium",
        "utm_campaign",
        "utm_content",
        "utm_term"
    ];

    function getCookie(name) {
        const prefix = name + "=";
        const cookies = document.cookie.split(";");

        for (let i = 0; i < cookies.length; i++) {
            let cookie = cookies[i].trim();

            if (cookie.indexOf(prefix) === 0) {
                return decodeURIComponent(
                    cookie.substring(prefix.length)
                );
            }
        }

        return null;
    }

    function setCookie(name, value, days) {
        const expires = new Date();

        expires.setTime(
            expires.getTime() +
            days * 24 * 60 * 60 * 1000
        );

        document.cookie =
            name +
            "=" +
            encodeURIComponent(value) +
            ";expires=" +
            expires.toUTCString() +
            ";path=/" +
            ";SameSite=Lax" +
            (window.location.protocol === "https:"
                ? ";Secure"
                : "");
    }

    function getUtmParameters() {
        const params = new URLSearchParams(
            window.location.search
        );

        const utm = {};

        UTM_PARAMETERS.forEach(function (parameter) {
            const value = params.get(parameter);

            if (value !== null && value.trim() !== "") {
                utm[parameter] = value.trim();
            }
        });

        return utm;
    }

    function hasUtmValues(utm) {
        return Object.keys(utm).length > 0;
    }

    function saveUtm(utm) {
        setCookie(
            COOKIE_NAME,
            JSON.stringify(utm),
            COOKIE_DAYS
        );
    }

    function init() {
        const currentUtm = getUtmParameters();

        if (hasUtmValues(currentUtm)) {
            saveUtm(currentUtm);
            return;
        }

        const existingCookie = getCookie(COOKIE_NAME);

        if (existingCookie) {
            try {
                const existingUtm = JSON.parse(existingCookie);

                saveUtm(existingUtm);
            }
            catch (error) {
                console.error("Invalid UTM cookie:", error);
            }
        }
        else {
            
            saveUtm({});
        }
    }

    window.UtmProcessor = {
        init: init,
        getUtmParameters: getUtmParameters
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () {
            init();
        });
    } else {
        init();
    }

})(window, document);