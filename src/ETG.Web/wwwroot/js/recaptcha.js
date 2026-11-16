/**
 * Loads the Google reCAPTCHA v3 script dynamically if not already loaded.
 * @param {string} siteKey
 * @returns {Promise<void>}
 */
function loadGoogleRecaptchaScript(siteKey) {
  return new Promise((resolve) => {
    if (window.grecaptcha && window.grecaptcha.execute) {
      resolve();
      return;
    }

    if (!siteKey) {
      resolve();
      return;
    }

    const existingScript = document.querySelector('script[src*="google.com/recaptcha/api.js"]');
    if (existingScript) {
      if (window.grecaptcha) {
        window.grecaptcha.ready(() => resolve());
      } else {
        existingScript.addEventListener("load", () => {
          if (window.grecaptcha) {
            window.grecaptcha.ready(() => resolve());
          } else {
            resolve();
          }
        });
      }
      return;
    }

    const script = document.createElement("script");
    script.src = `https://www.google.com/recaptcha/api.js?render=${encodeURIComponent(siteKey)}`;
    script.async = true;
    script.defer = true;
    script.onload = () => {
      if (window.grecaptcha) {
        window.grecaptcha.ready(() => resolve());
      } else {
        resolve();
      }
    };
    script.onerror = () => {
      resolve();
    };
    document.head.appendChild(script);
  });
}

/**
 * Validates reCAPTCHA and returns the token.
 * @param {string} action - The reCAPTCHA v3 action name (e.g. 'contact_us_submit')
 * @param {string} [siteKey] - Optional Google site key. Falls back to DOM data attributes or meta tag.
 * @returns {Promise<string>} Token string, or empty string if disabled/failed.
 */
export async function validateCaptcha(action = "submit", siteKey = "") {
  let resolvedSiteKey = siteKey;

  if (!resolvedSiteKey) {
    const meta = document.querySelector('meta[name="captchaSiteKey"]');
    if (meta instanceof HTMLMetaElement && meta.content) {
      resolvedSiteKey = meta.content;
    }
  }

  if (!resolvedSiteKey) {
    const el = document.querySelector('[data-site-key], [data-sitekey]');
    if (el) {
      resolvedSiteKey = el.getAttribute('data-site-key') || el.getAttribute('data-sitekey') || "";
    }
  }

  // If no site key is configured (e.g. dev environment), resolve empty token gracefully
  if (!resolvedSiteKey) {
    return "";
  }

  try {
    await loadGoogleRecaptchaScript(resolvedSiteKey);

    const grecaptcha = window.grecaptcha;
    if (!grecaptcha || typeof grecaptcha.execute !== "function") {
      return "";
    }

    return await new Promise((resolve) => {
      grecaptcha.ready(async () => {
        try {
          const token = await grecaptcha.execute(resolvedSiteKey, { action });
          resolve(token || "");
        } catch (err) {
          resolve("");
        }
      });
    });
  } catch (err) {
    return "";
  }
}

/**
 * Attaches reCAPTCHA handling to a form submit lifecycle.
 */
export function initCaptcha({
  formElemID,
  fieldElemID,
  fieldName = "g-recaptcha-response",
  actionName = "submit",
  siteKey = "",
  onValidate = () => true,
  onSubmit = () => {}
}) {
  const formEl =
    (formElemID ? document.getElementById(formElemID) || document.querySelector(`#${formElemID}`) : null) ||
    (fieldElemID ? document.getElementById(fieldElemID)?.closest("form") : null) ||
    document.querySelector(`input[name="${fieldName}"]`)?.closest("form") ||
    document.querySelector(".recaptcha")?.closest("form");

  if (!(formEl instanceof HTMLFormElement)) {
    return;
  }

  // Resolve site key if not provided directly
  let resolvedSiteKey = siteKey;
  if (!resolvedSiteKey) {
    resolvedSiteKey =
      formEl.querySelector('[data-site-key]')?.getAttribute('data-site-key') ||
      formEl.querySelector('[data-sitekey]')?.getAttribute('data-sitekey') ||
      document.querySelector('[data-site-key], [data-sitekey]')?.getAttribute('data-site-key') ||
      document.querySelector('meta[name="captchaSiteKey"]')?.getAttribute('content') ||
      "";
  }

  // Pre-load reCAPTCHA script in the background so it's ready when user submits
  if (resolvedSiteKey) {
    loadGoogleRecaptchaScript(resolvedSiteKey);
  }

  formEl.addEventListener("submit", async function (event) {
    // 1. Run client-side validation first
    if (typeof onValidate === "function" && !onValidate(formEl)) {
      event.preventDefault();
      return;
    }

    // 2. Prevent default to fetch the token
    event.preventDefault();

    let tokenInput = formEl.querySelector(`[name="${fieldName}"]`);
    if (!tokenInput && fieldElemID) {
      tokenInput = document.getElementById(fieldElemID);
    }

    const actionInput =
      formEl.querySelector('[name="g-recaptcha-action"]') ||
      document.getElementById("recaptchaAction");
    if (actionInput && actionName) {
      actionInput.value = actionName;
    }

    // 3. Fetch token via validateCaptcha
    const token = await validateCaptcha(actionName, resolvedSiteKey);

    if (tokenInput && token) {
      tokenInput.value = token;
    }

    // 4. Trigger the custom submit callback or standard form submit
    formEl.dispatchEvent(new CustomEvent("captchaReady", { detail: { token, action: actionName } }));

    if (typeof onSubmit === "function") {
      onSubmit(event, token);
    } else {
      HTMLFormElement.prototype.submit.call(formEl);
    }
  });
}

// Make accessible on window object for standard script tags
if (typeof window !== "undefined") {
  window.validateCaptcha = validateCaptcha;
  window.initCaptcha = initCaptcha;
}
