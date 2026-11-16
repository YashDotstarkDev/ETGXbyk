/**
 * Entire Travel Group - Client Payment Gateway Handler (TravelPay)
 */
(function ($) {
    'use strict';

    $(document).ready(function () {
        const $form = $('#client-payment-form');
        if (!$form.length) return;

        const $submitBtn = $form.find('#btn-pay-now');
        const $errorBanner = $('#payment-error-banner');

        function showError(msg) {
            $errorBanner.text(msg).removeClass('d-none');
            $('html, body').animate({
                scrollTop: $errorBanner.offset().top - 120
            }, 300);
        }

        function clearErrors() {
            $errorBanner.text('').addClass('d-none');
            $form.find('.is-invalid').removeClass('is-invalid');
        }

        // Clear inline invalid state on input change
        $form.find('input').on('input change', function () {
            $(this).removeClass('is-invalid');
        });

        function setLoading(isLoading) {
            if (isLoading) {
                $submitBtn.prop('disabled', true).addClass('loading');
                $submitBtn.find('.btn-text').addClass('d-none');
                $submitBtn.find('.btn-spinner').removeClass('d-none');
            } else {
                $submitBtn.prop('disabled', false).removeClass('loading');
                $submitBtn.find('.btn-text').removeClass('d-none');
                $submitBtn.find('.btn-spinner').addClass('d-none');
            }
        }

        function validateEmail(email) {
            return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
        }

        $form.on('submit', async function (e) {
            e.preventDefault();
            clearErrors();

            const firstName = $.trim($('#payment-firstname').val());
            const lastName = $.trim($('#payment-lastname').val());
            const email = $.trim($('#payment-email').val());
            const phone = $.trim($('#payment-phone').val());
            const invoiceRef = $.trim($('#payment-invoiceref').val());
            const rawAmount = $.trim($('#payment-amount').val()).replace(/[^0-9.]/g, '');
            const amount = parseFloat(rawAmount);
            const acceptTerms = $('#payment-accept-terms').is(':checked');

            let hasErrors = false;
            let firstInvalid = null;

            function checkField(condition, $field, errorMsg) {
                if (condition) {
                    $field.addClass('is-invalid');
                    if (!hasErrors) {
                        hasErrors = true;
                        firstInvalid = $field;
                        showError(errorMsg);
                    }
                }
            }

            // Inline validations matching Kentico 13 prompts
            checkField(!firstName, $('#payment-firstname'), 'Please enter first name.');
            checkField(!lastName, $('#payment-lastname'), 'Please enter last name.');
            checkField(!email || !validateEmail(email), $('#payment-email'), 'Please enter a valid email address.');
            checkField(!invoiceRef, $('#payment-invoiceref'), 'Please enter your Invoice Booking Reference (e.g. ETG123456).');
            checkField(isNaN(amount) || amount <= 0, $('#payment-amount'), 'Please enter a valid payment amount greater than $0.00.');
            checkField(!acceptTerms, $('#payment-accept-terms'), 'Please agree to the Terms & Conditions and Payment Conditions to proceed.');

            if (hasErrors) {
                if (firstInvalid) {
                    firstInvalid.focus();
                }
                return;
            }

            setLoading(true);

            // ReCAPTCHA v3 handling via shared recaptcha.js
            let recaptchaToken = '';
            try {
                const { validateCaptcha } = await import('/js/recaptcha.js');
                const siteKey = $form.find('.recaptcha').attr('data-site-key') || '';
                recaptchaToken = await validateCaptcha('client_payment_submit', siteKey);
                const $tokenInput = $form.find('[name="g-recaptcha-response"]');
                if ($tokenInput.length && recaptchaToken) {
                    $tokenInput.val(recaptchaToken);
                }
            } catch (err) {
                console.warn('ReCaptcha execution error:', err);
            }

            const payload = {
                firstName: firstName,
                lastName: lastName,
                email: email,
                phone: phone,
                invoiceReference: invoiceRef,
                amount: amount,
                acceptTerms: acceptTerms,
                recaptchaToken: recaptchaToken
            };

            try {
                const response = await fetch('/api/payment/create', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();

                if (!response.ok || !result.success) {
                    showError(result.message || 'Unable to initialize payment. Please verify your details and try again.');
                    setLoading(false);
                    return;
                }

                const tp = result.travelPay;
                if (!tp) {
                    showError('Unexpected response from the payment server. Please try again.');
                    setLoading(false);
                    return;
                }

                // Track active payment session for multi-stage auditing
                const activePayment = {
                    paymentId: result.paymentId,
                    mupid: tp.customerReference || ('P' + result.paymentId),
                    invoiceReference: payload.invoiceReference,
                    amount: payload.amount,
                    lastMethod: 'Credit Card',
                    isSettled: false
                };

                // Handler when TravelPay modal is closed before settlement
                window.onTravelPayClosed = function () {
                    if (!activePayment || activePayment.isSettled) return;
                    activePayment.isSettled = true;

                    const body = {
                        paymentId: activePayment.paymentId,
                        mupid: activePayment.mupid,
                        invoiceReference: activePayment.invoiceReference,
                        amount: activePayment.amount,
                        stage: 'PaymentModalDecline',
                        paymentMethod: activePayment.lastMethod,
                        failureCode: '2003',
                        failureReason: 'The transaction was declined or modal closed without completion',
                        additionalDetails: 'Customer closed modal after payment attempt without successful settlement'
                    };

                    try {
                        fetch('/api/payment/log-stage', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(body),
                            keepalive: true
                        });
                    } catch (e) {
                        console.warn('Error logging modal close:', e);
                    }

                    showError('Your payment was not completed or was declined. If your card was declined, please verify your card details or try another card.');
                };

                // Catch modal close button clicks and dismissal in parent window
                $(document).off('click.zpClose').on('click.zpClose', '.modal [data-bs-dismiss="modal"], .modal .btn-close, .modal .close, [data-dismiss="modal"], .modal-payment [data-bs-dismiss="modal"]', function () {
                    if (window.onTravelPayClosed) {
                        window.onTravelPayClosed();
                    }
                });

                $(document).off('hidden.bs.modal.zpClose').on('hidden.bs.modal.zpClose', function () {
                    if (window.onTravelPayClosed) {
                        window.onTravelPayClosed();
                    }
                });

                function launchTravelPayModal() {
                    if (typeof $.zpPayment !== 'function') {
                        showError('TravelPay checkout modal is not available. Please refresh the page and try again.');
                        setLoading(false);
                        return;
                    }

                    if (!tp.apiKey || !tp.fingerprint || !tp.merchantCode) {
                        showError('Payment gateway configuration is missing on the server (TravelPay ApiKey or MerchantCode is empty). Please check server configuration.');
                        setLoading(false);
                        return;
                    }

                    // Build plugin URL — do NOT append /online/v5 if it is already present
                    let pluginUrl = (tp.apiUrl || '').trim().replace(/\/+$/, '');
                    if (pluginUrl && !/\/online\/v/i.test(pluginUrl)) {
                        pluginUrl += '/online/v5';
                    }

                    // Apple Pay requires HTTPS; allow Google Pay on both HTTP and HTTPS
                    const isHttps = window.location.protocol === 'https:';

                    // Parameters matched to the production URL format:
                    // https://pay.travelpay.com.au/online/v5/ENTIRETRV/Authorise?hideHeader=true&mode=0&...
                    $.zpPayment({
                        action: 'Authorise',
                        url: pluginUrl,
                        apiKey: tp.apiKey,
                        fingerprint: tp.fingerprint,
                        merchantCode: tp.merchantCode,
                        redirectUrl: tp.redirectUrl,

                        // --- Display & Mode (matching prod) ---
                        mode: tp.mode ?? 0,
                        userMode: 0,
                        displayMode: 0,
                        hideHeader: true,

                        // --- Payment method flags (matching prod exactly) ---
                        allowBankAcOneOffPayment: false,
                        allowPayIdOneOffPayment: false,
                        allowApplePayOneOffPayment: isHttps,
                        allowGooglePayOneOffPayment: true,
                        allowUnionPayOneOffPayment: true,
                        allowAliPayPlusOneOffPayment: true,

                        // --- Fees & Terms (matching prod) ---
                        hideTermsAndConditions: true,
                        showFeeOnTokenising: false,
                        showFailedPaymentFeeOnTokenising: false,
                        overrideFeePayer: 0,

                        // --- Customer / Order details ---
                        customerName: tp.customerName,
                        customerReference: tp.customerReference,
                        merchantUniquePaymentId: tp.customerReference,
                        paymentAmount: tp.paymentAmount,
                        timeStamp: tp.timestamp,
                        title: 'Pay Now',
                        customerEmail: tp.customerEmail,

                        // --- Email notifications (matching prod) ---
                        sendConfirmationEmailToCustomer: false,
                        sendConfirmationEmailToMerchant: true,

                        // --- JS Plugin flag (matching prod) ---
                        isJsPlugin: true,

                        // The Zenith SDK serializes ALL options into the URL via .toString().
                        // No spaces in function — must produce: onPluginClose=function(){}
                        onPluginClose: function(){window.onTravelPayClosed&&window.onTravelPayClosed();}
                    }).open();

                    setLoading(false);
                }

                // Launch modal if script is ready, or dynamically inject it
                if (typeof $.zpPayment === 'function') {
                    launchTravelPayModal();
                } else {
                    console.log('Loading TravelPay script dynamically from official CDN...');
                    const cdnScript = 'https://cdn.travelpay.com.au/js/zenpay.payment.bs5.js';
                    $.getScript(cdnScript)
                        .done(function () {
                            launchTravelPayModal();
                        })
                        .fail(function (jqxhr, settings, exception) {
                            console.error('Failed to load TravelPay script:', exception);
                            showError('Unable to load payment gateway. Please check that scripts and popups are allowed.');
                            setLoading(false);
                        });
                }
            } catch (err) {
                console.error('Payment error:', err);
                showError('A network error occurred. Please check your connection and try again.');
                setLoading(false);
            }
        });
    });
})(jQuery);
