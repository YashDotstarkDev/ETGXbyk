import { validateCaptcha } from "./recaptcha.js";

document.querySelectorAll("[data-unsubscribe-form]").forEach(form => {
    if (form.dataset.initialized === "true") {
        return;
    }

    form.dataset.initialized = "true";

    const tokenInput = form.querySelector("[data-captcha-token]");
    const errorContainer = form.querySelector("[data-client-error]");

    let submitting = false;

    function setCommand(value) {
        let input = form.querySelector("[data-command-input]");

        if (!input) {
            input = document.createElement("input");
            input.type = "hidden";
            input.name = "command";
            input.dataset.commandInput = "true";

            form.appendChild(input);
        }

        input.value = value;
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();

        if (submitting) {
            return;
        }

        const command = event.submitter?.value === "cancel"
            ? "cancel"
            : "save";

        errorContainer.textContent = "";

        if (command === "cancel") {
            submitting = true;
            setCommand("cancel");

            HTMLFormElement.prototype.submit.call(form);
            return;
        }

        if (!form.reportValidity()) {
            return;
        }

        submitting = true;

        let timer;

        try {
            tokenInput.value = "";

            if (form.dataset.skipCaptcha !== "true") {
                const timeout = new Promise((_, reject) => {
                    timer = window.setTimeout(() => {
                        reject(new Error("CAPTCHA timed out."));
                    }, 15000);
                });

                tokenInput.value = await Promise.race([
                    validateCaptcha("unsubscribe_reason"),
                    timeout
                ]);
            }

            setCommand("save");

            HTMLFormElement.prototype.submit.call(form);
        } catch {
            submitting = false;
            tokenInput.value = "";

            errorContainer.textContent =
                "Security verification could not complete. Please try again.";
        } finally {
            if (timer !== undefined) {
                window.clearTimeout(timer);
            }
        }
    });
});