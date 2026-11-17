// Entire Travel Group — small UI helpers
(function () {
  'use strict';

  // Add scrolled class on nav for subtle shadow change
  const header = document.querySelector('.site-header');
  if (header) {
    const onScroll = () => {
      if (window.scrollY > 10) header.classList.add('scrolled');
      else header.classList.remove('scrolled');
    };
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
  }

  // Smooth scroll for in-page anchors
  document.querySelectorAll('a[href^="#"]').forEach((link) => {
    link.addEventListener('click', (e) => {
      const id = link.getAttribute('href');
      if (id.length > 1) {
        const target = document.querySelector(id);
        if (target) {
          e.preventDefault();
          target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      }
    });
  });
})();

document.querySelectorAll(".footer-dropdown").forEach(item => {
  item.addEventListener("click", function (e) {

    if (window.innerWidth <= 768) {

      let parent = this.parentElement;

      // close others (optional but recommended)
      document.querySelectorAll(".footer-menu-item").forEach(el => {
        if (el !== parent) el.classList.remove("active");
      });

      // toggle current
      parent.classList.toggle("active");

    }

  });
});
const toggleBtn = document.getElementById("menuToggle");
const mobileMenu = document.getElementById("mobileMenuCustom");
const icon = document.getElementById("menuIcon");

if (toggleBtn) {
  toggleBtn.addEventListener("click", () => {
    mobileMenu.classList.toggle("open");

    if (mobileMenu.classList.contains("open")) {
      icon.classList.remove("bi-list");
      icon.classList.add("bi-x"); // cross icon
    } else {
      icon.classList.remove("bi-x");
      icon.classList.add("bi-list");
    }
  });
}

// --- CONTACT FORM VALIDATION ---
// (function () {
//   'use strict'
//   const contactForm = document.getElementById('contactForm');
//   const formSuccessMessage = document.getElementById('formSuccessMessage');

//   if (contactForm) {
//     contactForm.addEventListener('submit', function (event) {
//       event.preventDefault();
//       event.stopPropagation();

//       if (contactForm.checkValidity()) {
//         formSuccessMessage.classList.remove('d-none');
//         contactForm.reset();
//         contactForm.classList.remove('was-validated');
//         setTimeout(() => {
//           formSuccessMessage.classList.add('d-none');
//         }, 8000);
//       } else {
//         contactForm.classList.add('was-validated');
//       }
//     }, false);
//   }
// })();

// --- TABS BAR HORIZONTAL SCROLL FOR DESKTOP ---
(function() {
  'use strict';
  const tabsBarInner = document.querySelector('.pkgd-tabs-bar__inner');
  if (!tabsBarInner) return;

  let isDown = false;
  let startX = 0;
  let scrollLeft = 0;
  const isDesktop = window.innerWidth > 768;

  // Only enable drag on desktop
  if (!isDesktop) return;

  // Mouse wheel scroll
  tabsBarInner.addEventListener('wheel', (e) => {
    if (tabsBarInner.scrollWidth > tabsBarInner.clientWidth) {
      e.preventDefault();
      tabsBarInner.scrollLeft += (e.deltaY > 0 ? 50 : -50);
    }
  }, { passive: false });

  // Drag to scroll (desktop only)
  tabsBarInner.addEventListener('mousedown', (e) => {
    isDown = true;
    startX = e.pageX - tabsBarInner.offsetLeft;
    scrollLeft = tabsBarInner.scrollLeft;
    tabsBarInner.style.cursor = 'grabbing';
  });

  tabsBarInner.addEventListener('mouseleave', () => {
    isDown = false;
    tabsBarInner.style.cursor = 'grab';
  });

  tabsBarInner.addEventListener('mouseup', () => {
    isDown = false;
    tabsBarInner.style.cursor = 'grab';
  });

  tabsBarInner.addEventListener('mousemove', (e) => {
    if (!isDown) return;
    e.preventDefault();
    const x = e.pageX - tabsBarInner.offsetLeft;
    const walk = (x - startX) * 1.5;
    tabsBarInner.scrollLeft = scrollLeft - walk;
  });
})();
(function () {
  'use strict';
  const slider = document.querySelector('.region-scroll-row');
  if (!slider) return;

  let isDown = false;
  let startX;
  let scrollLeft;
  let animationId;
  let isHovering = false;

  function autoScroll() {
    if (!isDown && !isHovering && slider.scrollWidth > slider.clientWidth) {
      slider.scrollLeft += 1.5;
      if (Math.ceil(slider.scrollLeft) >= slider.scrollWidth - slider.clientWidth) {
        slider.scrollLeft = 0;
      }
    }
    animationId = requestAnimationFrame(autoScroll);
  }

  animationId = requestAnimationFrame(autoScroll);

  slider.addEventListener('mouseenter', () => isHovering = true);
  slider.addEventListener('mouseleave', () => {
    isHovering = false;
    isDown = false;
    slider.classList.remove('grabbing');
  });

  slider.addEventListener('touchstart', () => isHovering = true, { passive: true });
  slider.addEventListener('touchend', () => isHovering = false);

  slider.addEventListener('mousedown', (e) => {
    isDown = true;
    isHovering = true;
    slider.classList.add('grabbing');
    startX = e.pageX - slider.offsetLeft;
    scrollLeft = slider.scrollLeft;
  });

  slider.addEventListener('mouseup', () => {
    isDown = false;
    isHovering = false;
    slider.classList.remove('grabbing');
  });

  slider.addEventListener('mousemove', (e) => {
    if (!isDown) return;
    e.preventDefault();
    const x = e.pageX - slider.offsetLeft;
    const walk = (x - startX) * 1.5;
    slider.scrollLeft = scrollLeft - walk;
  });

  const imagesAndLinks = slider.querySelectorAll('img, a');
  imagesAndLinks.forEach(el => {
    el.addEventListener('dragstart', (e) => e.preventDefault());
  });
})();
// Dual Range Slider logic
document.addEventListener("DOMContentLoaded", function() {
  document.querySelectorAll('.dual-slider-wrap').forEach(wrap => {
    const rangeMin = wrap.querySelector('.range-min');
    const rangeMax = wrap.querySelector('.range-max');
    const sliderFill = wrap.querySelector('.slider-fill');
    const valMinLabel = wrap.querySelector('.val-min');
    const valMaxLabel = wrap.querySelector('.val-max');
    const prefix = wrap.getAttribute('data-prefix') || '';
    const suffix = wrap.getAttribute('data-suffix') || '';

    const updateSlider = () => {
      let minVal = parseInt(rangeMin.value);
      let maxVal = parseInt(rangeMax.value);

      if (minVal > maxVal) {
        let tmp = maxVal;
        maxVal = minVal;
        minVal = tmp;
      }

      const maxTotal = parseInt(rangeMin.max);
      const minTotal = parseInt(rangeMin.min);
      
      const leftPercent = ((minVal - minTotal) / (maxTotal - minTotal)) * 100;
      const widthPercent = ((maxVal - minVal) / (maxTotal - minTotal)) * 100;

      sliderFill.style.left = leftPercent + '%';
      sliderFill.style.width = widthPercent + '%';

      valMinLabel.innerText = prefix + minVal.toLocaleString() + suffix;
      valMaxLabel.innerText = prefix + maxVal.toLocaleString() + suffix;
    };

    rangeMin.addEventListener('input', () => {
      let minVal = parseInt(rangeMin.value);
      let maxVal = parseInt(rangeMax.value);
      if (minVal >= maxVal) {
        rangeMin.value = maxVal - 1;
      }
      updateSlider();
    });

    rangeMax.addEventListener('input', () => {
      let minVal = parseInt(rangeMin.value);
      let maxVal = parseInt(rangeMax.value);
      if (maxVal <= minVal) {
        rangeMax.value = minVal + 1;
      }
      updateSlider();
    });

    updateSlider();
  });
});


  // (function() {
  //     const target = document.querySelector('.connect-bar.text-center');
  //     const footer = document.querySelector('footer.etg-footer');
      

  //     if (!target || !footer) return;

  //     // Create a spacer to reserve space when target is fixed
  //     const spacer = document.createElement('div');
  //     spacer.style.display = 'none';
  //     target.parentNode.insertBefore(spacer, target.nextSibling);

  //     let ticking = false;
  //     let footerDocTop = 0;   // footer's top position relative to the full document, cached
  //     let barHeight = 0;      // bar's own height, cached (measured while it's in normal flow)
  //     let currentState = null; // 'top' | 'landed' | 'fixed' — avoid re-writing styles every frame


  //     // Measure the bar's natural height once, while it is NOT fixed/hidden,
  //     // so the spacer never has to read a height that we ourselves just changed.
  //     const measureBarHeight = () => {
  //         const wasPosition = target.style.position;
  //         const wasOpacity = target.style.opacity;
  //         const wasVisibility = target.style.visibility;

  //         // Temporarily force it into normal flow, visible, to get a true height
  //         target.style.position = 'static';
  //         target.style.visibility = 'hidden';
  //         target.style.opacity = '0';
  //         barHeight = target.getBoundingClientRect().height;

  //         // restore whatever it was
  //         target.style.position = wasPosition;
  //         target.style.opacity = wasOpacity;
  //         target.style.visibility = wasVisibility;
  //     };

  //     // Cache the footer's position relative to the document (not the viewport).
  //     // This is the piece that removes the circular dependency: we no longer ask
  //     // "where is the footer right now on screen" (which our own spacer affects),
  //     // we ask "where does the footer live in the document" (which it doesn't).
  //     const measureFooterPosition = () => {
  //         const rect = footer.getBoundingClientRect();
  //         footerDocTop = rect.top + window.scrollY;
  //         measureBarHeight();
  //     };

  //     const syncSpacer = (active) => {
  //         if (active) {
  //             spacer.style.height = barHeight + 'px';
  //             spacer.style.display = 'block';
  //         } else {
  //             spacer.style.display = 'none';
  //         }
  //     };

  //     const applyState = (state, viewportHeight) => {
  //         if (state === currentState) return; // no-op if nothing changed, avoids extra reflows
  //         currentState = state;

  //         if (state === 'top') {
  //             target.style.position = 'static';
  //             target.style.opacity = '0';
  //             target.style.visibility = 'hidden';
  //             target.style.pointerEvents = 'none';
  //             target.style.width = '';
  //             syncSpacer(false);
  //         } else if (state === 'landed') {
  //             target.style.position = 'static';
  //             target.style.opacity = '1';
  //             target.style.visibility = 'visible';
  //             target.style.pointerEvents = 'auto';
  //             target.style.width = '100%';
  //             syncSpacer(false);
  //         } else {
  //             // fixed
  //             target.style.position = 'fixed';
  //             target.style.bottom = '0 ';
  //             target.style.left = '0';
  //             target.style.width = '100%';
  //             target.style.zIndex = '9999';
  //             target.style.opacity = '1';
  //             target.style.visibility = 'visible';
  //             target.style.pointerEvents = 'auto';
  //             target.style.backgroundColor = '#ffff';
  //             target.style.transition = 'opacity 0.3s ease';
  //             syncSpacer(true);
  //         }
  //     };

  //     const computeState = () => {
  //         // visualViewport.height is more stable on mobile than window.innerHeight,
  //         // which changes as the browser chrome collapses/expands during scroll.
  //         const viewportHeight = (window.visualViewport && window.visualViewport.height)
  //             ? window.visualViewport.height
  //             : window.innerHeight;

  //         const scrollY = window.scrollY;
  //         const threshold = viewportHeight / 2;

  //         // "Footer bottom reaches viewport bottom" restated in document coordinates:
  //         // footer's bottom (footerDocTop + footer height) <= scrollY + viewportHeight
  //         const footerHeight = footer.getBoundingClientRect().height; // safe: footer's own height, not affected by our spacer
  //         const footerBottomDoc = footerDocTop + footerHeight;
  //         const landed = footerBottomDoc <= scrollY + viewportHeight;

  //         let state;
  //         if (scrollY < threshold) {
  //             state = 'top';
  //         } else if (landed) {
  //             state = 'landed';
  //         } else {
  //             state = 'fixed';
  //         }

  //         applyState(state, viewportHeight);
  //     };

  //     const handleScroll = () => {
  //         if (ticking) return;
  //         ticking = true;
  //         requestAnimationFrame(() => {
  //             computeState();
  //             ticking = false;
  //         });
  //     };

  //     let resizeTimer = null;
  //     const handleResize = () => {
  //         clearTimeout(resizeTimer);
  //         resizeTimer = setTimeout(() => {
  //             // Re-measure footer position and bar height since layout may have changed
  //             // (e.g. orientation change, address bar collapse settling, content reflow).
  //             const prevState = currentState;
  //             currentState = null; // force re-apply after remeasure
  //             measureFooterPosition();
  //             computeState();
  //         }, 150);
  //     };

  //     // Initial measurement + state
  //     measureFooterPosition();
  //     computeState();

  //     window.addEventListener('scroll', handleScroll, { passive: true });
  //     window.addEventListener('resize', handleResize);
  //     if (window.visualViewport) {
  //         window.visualViewport.addEventListener('resize', handleResize);
  //     }
  // })();



  
  
(function() {
    const target = document.querySelector('.connect-bar.text-center');
    const footer = document.querySelector('footer.etg-footer');

    if (!target || !footer) return;

    // Always relocate the bar to sit right after the footer,
    // no matter where the component was rendered on the page.
    if (footer.nextElementSibling !== target) {
        footer.insertAdjacentElement('afterend', target);
    }

    // Create a spacer to reserve space when target is fixed
    const spacer = document.createElement('div');
    spacer.style.display = 'none';
    target.parentNode.insertBefore(spacer, target.nextSibling);

    let ticking = false;
    let footerDocTop = 0;   // footer's top position relative to the full document, cached
    let barHeight = 0;      // bar's own height, cached (measured while it's in normal flow)
    let currentState = null; // 'top' | 'landed' | 'fixed' — avoid re-writing styles every frame

    // Measure the bar's natural height once, while it is NOT fixed/hidden,
    // so the spacer never has to read a height that we ourselves just changed.
    const measureBarHeight = () => {
        const wasPosition = target.style.position;
        const wasOpacity = target.style.opacity;
        const wasVisibility = target.style.visibility;

        // Temporarily force it into normal flow, visible, to get a true height
        target.style.position = 'static';
        target.style.visibility = 'hidden';
        target.style.opacity = '0';
        barHeight = target.getBoundingClientRect().height;

        // restore whatever it was
        target.style.position = wasPosition;
        target.style.opacity = wasOpacity;
        target.style.visibility = wasVisibility;
    };

    // Cache the footer's position relative to the document (not the viewport).
    // This is the piece that removes the circular dependency: we no longer ask
    // "where is the footer right now on screen" (which our own spacer affects),
    // we ask "where does the footer live in the document" (which it doesn't).
    const measureFooterPosition = () => {
        const rect = footer.getBoundingClientRect();
        footerDocTop = rect.top + window.scrollY;
        measureBarHeight();
    };

    const syncSpacer = (active) => {
        if (active) {
            spacer.style.height = barHeight + 'px';
            spacer.style.display = 'block';
        } else {
            spacer.style.display = 'none';
        }
    };

    const applyState = (state, viewportHeight) => {
        if (state === currentState) return; // no-op if nothing changed, avoids extra reflows
        currentState = state;

        if (state === 'top') {
            target.style.position = 'relative';
            target.style.opacity = '0';
            target.style.visibility = 'hidden';
            target.style.pointerEvents = 'none';
            target.style.width = '';
            syncSpacer(false);
        } else if (state === 'landed') {
            target.style.position = 'relative';
            target.style.opacity = '1';
            target.style.visibility = 'visible';
            target.style.pointerEvents = 'auto';
            target.style.width = '100%';
            syncSpacer(false);
        } else {
            // fixed
            target.style.position = 'fixed';
            target.style.bottom = '0';
            target.style.left = '0';
            target.style.width = '100%';
            target.style.zIndex = '9989';
            target.style.opacity = '1';
            target.style.visibility = 'visible';
            target.style.pointerEvents = 'auto';
            target.style.backgroundColor = '#ca568e';
            target.style.transition = 'opacity 0.3s ease';
            syncSpacer(true);
        }
    };

    const computeState = () => {
        // visualViewport.height is more stable on mobile than window.innerHeight,
        // which changes as the browser chrome collapses/expands during scroll.
        const viewportHeight = (window.visualViewport && window.visualViewport.height)
            ? window.visualViewport.height
            : window.innerHeight;

        const scrollY = window.scrollY;
        const threshold = viewportHeight / 2;

        // "Footer bottom reaches viewport bottom" restated in document coordinates:
        // footer's bottom (footerDocTop + footer height) <= scrollY + viewportHeight
        const footerHeight = footer.getBoundingClientRect().height; // safe: footer's own height, not affected by our spacer
        const footerBottomDoc = footerDocTop + footerHeight;
        const landed = footerBottomDoc <= scrollY + viewportHeight;

        let state;
        if (scrollY < threshold) {
            state = 'top';
        } else if (landed) {
            state = 'landed';
        } else {
            state = 'fixed';
        }

        applyState(state, viewportHeight);
    };

    const handleScroll = () => {
        if (ticking) return;
        ticking = true;
        requestAnimationFrame(() => {
            computeState();
            ticking = false;
        });
    };

    let resizeTimer = null;
    const handleResize = () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {
            // Re-measure footer position and bar height since layout may have changed
            // (e.g. orientation change, address bar collapse settling, content reflow).
            const prevState = currentState;
            currentState = null; // force re-apply after remeasure
            measureFooterPosition();
            computeState();
        }, 150);
    };

    // Initial measurement + state
    measureFooterPosition();
    computeState();

    window.addEventListener('scroll', handleScroll, { passive: true });
    window.addEventListener('resize', handleResize);
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', handleResize);
    }
})();
(function initTickerMarquee() {
    var wrapper = document.querySelector('.ticker');
    var track = document.getElementById('tickerTrack');
    if (!wrapper || !track) return;

    var originalHTML = track.innerHTML;

    var build = function () {
        // Reset to the single original set first
        track.innerHTML = originalHTML;
        track.style.animation = 'none';

        var wrapperWidth = wrapper.getBoundingClientRect().width;
        var setWidth = track.getBoundingClientRect().width;

        if (setWidth === 0) return; // not rendered yet, bail safely

        // Keep cloning full sets until content is at least 2x the visible width
        // This guarantees there is always enough content to cover the gap on ANY screen size
        while (track.scrollWidth < wrapperWidth * 2) {
            track.insertAdjacentHTML('beforeend', originalHTML);
        }

        setWidth = originalHTML ? (track.scrollWidth / (track.children.length / (track.querySelectorAll('.marque-item').length / 3))) : setWidth;

        // Simpler & robust: measure width of exactly one original set by re-inserting it once more,
        // then use that as the exact scroll distance so the loop is always seamless regardless of clone count
        var singleSetWidth = (function () {
            var temp = document.createElement('div');
            temp.style.position = 'absolute';
            temp.style.visibility = 'hidden';
            temp.style.display = 'flex';
            temp.innerHTML = originalHTML;
            document.body.appendChild(temp);
            var w = temp.getBoundingClientRect().width;
            document.body.removeChild(temp);
            return w;
        })();

        // Duplicate the whole (possibly multi-set) track once more so it can loop by exactly singleSetWidth
        track.insertAdjacentHTML('beforeend', track.innerHTML);

        var distance = singleSetWidth;
        var duration = Math.max(10, distance / 50); // consistent speed regardless of content length

        // Inject a fresh keyframe with the exact pixel distance needed — guarantees zero gap
        var styleTag = document.getElementById('ticker-keyframes') || document.createElement('style');
        styleTag.id = 'ticker-keyframes';
        styleTag.innerHTML = '@keyframes tickerScroll { from { transform: translate3d(0, 0, 0); } to { transform: translate3d(-' + distance + 'px, 0, 0); } }';
        document.head.appendChild(styleTag);

        track.style.animation = 'tickerScroll ' + duration + 's linear infinite';
    };

    build();

    var resizeTimer;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(build, 200);
    });
})();






// Initialize Tom Select for dropdowns (e.g. Subscribe Now, NotFound, etc.)
(function () {
    document.querySelectorAll("select.tom-select").forEach((select, index) => {
        // 🔑 Prevent double-initialization and never run on wrapper divs or non-select elements
        if (!select || select.tagName !== "SELECT" || select.tomselect || select.closest(".ts-wrapper")) {
            return;
        }

        // Skip Algolia-managed dropdowns handled exclusively by dedicated page scripts
        if (select.id === "heroDestination" || select.id === "heroExperience" || select.id === "searchbox-destination" || select.id === "searchbox-experience") {
            return;
        }

        try {
            const ts = new TomSelect(select, {
                create: false,
                searchField: [],
                controlInput: null
            });

            const wrapper = select.closest(".select-wrapper");
            if (!wrapper) {
                // No wrapper on this select — skip arrow logic, but TomSelect still works fine
                return;
            }

            const arrow = wrapper.querySelector(".custom-arrow");
            if (!arrow) {
                return;
            }

            ts.control.appendChild(arrow);

            ts.on("dropdown_open", () => {
                arrow.classList.add("rotate");
                wrapper.classList.add("dropdown-open");
            });

            ts.on("dropdown_close", () => {
                arrow.classList.remove("rotate");
                wrapper.classList.remove("dropdown-open");
            });

        } catch (err) {
            console.error(`Tom Select init failed for element #${index}:`, err);
        }
    });
})();

/* ============================================================
   OPTIONAL EXTRAS ACCORDION FUNCTIONALITY (Single-Open)
   ============================================================ */
(function() {
    'use strict';

    // Get all accordion panels in the Optional Extras section (exclude hotels)
    // const optionalExtrasContainer = document.querySelector('.widget.package-accordion');
    const optionalExtrasContainer = document.querySelector('#pkg-optional-extras .widget.package-accordion');
    if (!optionalExtrasContainer) {
        return; // Exit if no optional extras accordion found
    }

    const accordionTitles = optionalExtrasContainer.querySelectorAll('.accordion-panel .title');

    if (accordionTitles.length === 0) {
        return; // Exit if no accordion titles found
    }

    accordionTitles.forEach(titleEl => {
        titleEl.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();

            // Get the content element next to the title
            const contentEl = this.nextElementSibling;

            if (!contentEl || !contentEl.classList.contains('content')) {
                return; // Exit if content element not found
            }

            // Check if this item is already active
            const isActive = this.classList.contains('active');

            // Close all other accordion items (single-open behavior)
            accordionTitles.forEach(otherTitle => {
                if (otherTitle !== this) {
                    otherTitle.classList.remove('active');
                    const otherContent = otherTitle.nextElementSibling;
                    if (otherContent && otherContent.classList.contains('content')) {
                        otherContent.classList.remove('active');
                        const otherInnerContent = otherContent.querySelector('.inner-content');
                        if (otherInnerContent) {
                            otherInnerContent.classList.add('hidden');
                            otherInnerContent.classList.remove('visible');
                        }
                        const otherIcon = otherTitle.querySelector('.dropdown.icon');
                        if (otherIcon) {
                            otherIcon.classList.remove('rotate');
                        }
                    }
                }
            });

            // Toggle the current item
            this.classList.toggle('active');
            contentEl.classList.toggle('active');

            // Make content visible and animate
            const innerContent = contentEl.querySelector('.inner-content');
            if (innerContent) {
                innerContent.classList.toggle('hidden');
                innerContent.classList.toggle('visible');
            }

            // Handle the dropdown icon rotation
            const dropdownIcon = this.querySelector('.dropdown.icon');
            if (dropdownIcon) {
                dropdownIcon.classList.toggle('rotate');
            }
        });
    });

    // Auto-open the first accordion item on page load
    const firstTitle = optionalExtrasContainer.querySelector('.accordion-panel .title');
    if (firstTitle && !firstTitle.classList.contains('active')) {
        firstTitle.classList.add('active');
        const firstContent = firstTitle.nextElementSibling;
        if (firstContent && firstContent.classList.contains('content')) {
            firstContent.classList.add('active');
            const innerContent = firstContent.querySelector('.inner-content');
            if (innerContent) {
                innerContent.classList.remove('hidden');
                innerContent.classList.add('visible');
            }
            const firstIcon = firstTitle.querySelector('.dropdown.icon');
            if (firstIcon) {
                firstIcon.classList.add('rotate');
            }
        }
    }
})();

/* ============================================================
   FREEDOM OF CHOICE & PACKAGE UPGRADES ACCORDION
   ============================================================ */
(function () {
    'use strict';

    const accordionSections = document.querySelectorAll(
        '#pkg-freedom-of-choice, #pkg-package-upgrades'
    );

    if (!accordionSections.length) return;

    accordionSections.forEach(section => {
        const titles = section.querySelectorAll('.accordion-panel > .title');

        titles.forEach(titleEl => {
            const contentEl = titleEl.nextElementSibling;

            if (!contentEl || !contentEl.classList.contains('content')) {
                return;
            }

            const innerContent = contentEl.querySelector('.inner-content');
            const icon = titleEl.querySelector('.dropdown.icon');

            // Both sections are closed by default
            titleEl.classList.remove('active');
            contentEl.classList.remove('active');

            if (innerContent) {
                innerContent.classList.add('hidden');
                innerContent.classList.remove('visible');
            }

            if (icon) {
                icon.classList.remove('rotate');
            }

            // Open or close when clicked
            titleEl.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopImmediatePropagation();

                const isActive = titleEl.classList.contains('active');

                titleEl.classList.toggle('active', !isActive);
                contentEl.classList.toggle('active', !isActive);

                if (innerContent) {
                    innerContent.classList.toggle('hidden', isActive);
                    innerContent.classList.toggle('visible', !isActive);
                }

                if (icon) {
                    icon.classList.toggle('rotate', !isActive);
                }
            }, true);
        });
    });
})();

/* ============================================================
   ACCOMMODATION ACCORDION FUNCTIONALITY
   ============================================================ */
(function() {
    'use strict';

    // Get the accommodation accordion container
    const accommodationContainer = document.querySelector('.widget.package-hotels');
    if (!accommodationContainer) {
        return; // Exit if no accommodation accordion found
    }

    const accommodationTitles = accommodationContainer.querySelectorAll('.accordion-panel .title');

    if (accommodationTitles.length === 0) {
        return; // Exit if no accommodation titles found
    }

    accommodationTitles.forEach(titleEl => {
        titleEl.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();

            // Get the content element next to the title
            const contentEl = this.nextElementSibling;

            if (!contentEl || !contentEl.classList.contains('content')) {
                return; // Exit if content element not found
            }

            // Toggle classes on title
            this.classList.toggle('active');

            // Toggle classes on content
            contentEl.classList.toggle('active');

            // Make content visible and animate
            const innerContent = contentEl.querySelector('.inner-content');
            if (innerContent) {
                innerContent.classList.toggle('hidden');
                innerContent.classList.toggle('visible');
            }

            // Handle the dropdown icon rotation
            const dropdownIcon = this.querySelector('.dropdown.icon');
            if (dropdownIcon) {
                dropdownIcon.classList.toggle('rotate');
            }
        });
    });
})();



/* ============================================================
   DYNAMIC TEXT EXPANSION FOR BARGE JOURNEYS SECTION
   Efficiently handles image visibility without infinite loops
   ============================================================ */
// (function() {
//     'use strict';

//     let lastState = null; // Track last state to avoid unnecessary updates

//     function checkAndUpdateLayout() {
//         // Find all rows with the barge journeys structure
//         document.querySelectorAll('.row.align-items-center.g-5').forEach(row => {
//             const textCol = row.querySelector('.col-lg-7');
//             const imageCol = row.querySelector('.col-lg-5.text-end');
//             const img = imageCol ? imageCol.querySelector('img') : null;

//             if (!textCol || !imageCol) return;

//             // Check if image exists and is visible
//             const isImageVisible = img && 
//                                    img.offsetParent !== null && 
//                                    window.getComputedStyle(img).display !== 'none' &&
//                                    window.getComputedStyle(img).visibility !== 'hidden';

//             // Only update if state changed (prevents excessive DOM writes)
//             const currentState = isImageVisible ? 'visible' : 'hidden';
//             if (lastState === currentState) return;
//             lastState = currentState;

//             if (isImageVisible) {
//                 // Image is visible - normal layout
//                 textCol.classList.remove('text-expand-full');
//                 imageCol.classList.remove('image-hidden');
//             } else {
//                 // Image is hidden or missing - expand text to full width
//                 textCol.classList.add('text-expand-full');
//                 imageCol.classList.add('image-hidden');
//             }
//         });
//     }

//     // Run on page load
//     if (document.readyState === 'loading') {
//         document.addEventListener('DOMContentLoaded', checkAndUpdateLayout);
//     } else {
//         checkAndUpdateLayout();
//     }

//     // Debounced resize handler - prevents excessive function calls
//     let resizeTimeout;
//     window.addEventListener('resize', () => {
//         clearTimeout(resizeTimeout);
//         resizeTimeout = setTimeout(checkAndUpdateLayout, 150);
//     }, { passive: true });

//     // One-time MutationObserver to detect style/visibility changes
//     // Only observes for a short time to avoid performance impact
//     let mutationTimeout;
//     const observer = new MutationObserver(() => {
//         checkAndUpdateLayout();
//     });

//     // Start observing after a brief delay to allow DOM to settle
//     setTimeout(() => {
//         document.querySelectorAll('.col-lg-5.text-end img').forEach(img => {
//             observer.observe(img, {
//                 attributes: true,
//                 attributeFilter: ['style'],
//                 subtree: false
//             });
//         });

//         // Stop observing after 5 seconds to save resources
//         clearTimeout(mutationTimeout);
//         mutationTimeout = setTimeout(() => {
//             observer.disconnect();
//         }, 5000);
//     }, 500);
// })();


document.addEventListener('DOMContentLoaded', function () {
  const rows = document.querySelectorAll('.row.Expansion');

  rows.forEach(function (row) {
    const imgCol = row.querySelector('.col-lg-5');
    const img = imgCol ? imgCol.querySelector('img') : null;

    function setNoImage() {
      row.classList.add('no-image');
    }

    function checkImage() {
      // No img tag or no src at all
      if (!img || !imgCol || !img.getAttribute('src')) {
        setNoImage();
        return;
      }

      // img tag exists but column or img is hidden via CSS/inline style
      if (
        getComputedStyle(imgCol).display === 'none' ||
        getComputedStyle(img).display === 'none'
      ) {
        setNoImage();
        return;
      }

      // img tag exists but image failed to load (broken src)
      if (img.complete && img.naturalWidth === 0) {
        setNoImage();
        return;
      }

      // Image is still loading — wait for onerror or onload
      img.addEventListener('error', setNoImage);
      img.addEventListener('load', function () {
        if (img.naturalWidth === 0) setNoImage();
      });
    }

    checkImage();

    const observer = new MutationObserver(checkImage);
    observer.observe(row, {
      attributes: true,
      childList: true,
      subtree: true,
      attributeFilter: ['style', 'class', 'src']
    });
  });
});


/* image gallery widget Custom css*/
(function initImageLightbox() {
    'use strict';

    var cells = Array.from(
        document.querySelectorAll('[data-lb]')
    ).filter(function (cell) { return cell.querySelector('img'); });

    var overlay = document.getElementById('lb');
    var strip = document.getElementById('lb-strip');
    var caption = document.getElementById('lb-caption');
    var btnClose = document.getElementById('lb-close');
    var btnPrev = document.getElementById('lb-prev');
    var btnNext = document.getElementById('lb-next');

    if (
        !cells.length ||
        !overlay ||
        !strip ||
        !caption ||
        !btnClose ||
        !btnPrev ||
        !btnNext
    ) {
        return;
    }

    if (overlay.dataset.lightboxInitialized === 'true') {
        return;
    }

    overlay.dataset.lightboxInitialized = 'true';

    var images = cells.map(function (cell) {
        var img = cell.querySelector('img');
        return {
            src: img.currentSrc || img.src,
            alt: img.alt || ''
        };
    });

    var currentIndex = 0;
    var touchStartX = 0;
    var SWIPE_THRESHOLD = 50;
    var previousBodyOverflow = '';

    images.forEach(function (imageData) {
        var slide = document.createElement('div');
        slide.className = 'lb-slide';
        var img = document.createElement('img');
        img.src = imageData.src;
        img.alt = imageData.alt;
        img.loading = 'lazy';
        slide.appendChild(img);
        strip.appendChild(slide);
    });

    var goTo = function (index) {
        currentIndex = (index + images.length) % images.length;
        strip.style.transform = 'translateX(-' + (currentIndex * 100) + '%)';
        caption.textContent = images[currentIndex].alt;
    };

    var openLightbox = function (index) {
        previousBodyOverflow = document.body.style.overflow;
        overlay.classList.add('lb-open');
        document.body.style.overflow = 'hidden';
        goTo(index);
    };

    var closeLightbox = function () {
        overlay.classList.remove('lb-open');
        document.body.style.overflow = previousBodyOverflow;
    };

    cells.forEach(function (cell, index) {
        cell.addEventListener('click', function () {
            openLightbox(index);
        });
    });

    btnPrev.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        goTo(currentIndex - 1);
    });

    btnNext.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        goTo(currentIndex + 1);
    });

    btnClose.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        closeLightbox();
    });

    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) {
            closeLightbox();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (!overlay.classList.contains('lb-open')) return;
        switch (e.key) {
            case 'Escape':
                closeLightbox();
                break;
            case 'ArrowLeft':
                goTo(currentIndex - 1);
                break;
            case 'ArrowRight':
                goTo(currentIndex + 1);
                break;
        }
    });

    overlay.addEventListener('touchstart', function (e) {
        if (e.changedTouches.length) {
            touchStartX = e.changedTouches[0].screenX;
        }
    }, { passive: true });

    overlay.addEventListener('touchend', function (e) {
        if (e.changedTouches.length) {
            var diffX = touchStartX - e.changedTouches[0].screenX;
            if (Math.abs(diffX) > SWIPE_THRESHOLD) {
                if (diffX > 0) {
                    goTo(currentIndex + 1);
                } else {
                    goTo(currentIndex - 1);
                }
            }
        }
    }, { passive: true });
})();
/*three column callouts widget custom js*/
(function () {
    'use strict';

    document.addEventListener('click', function (event) {

        const link = event.target.closest('.read-more-link');

        if (!link) {
            return;
        }

        const targetId = link.getAttribute('data-target');

        if (!targetId) {
            return;
        }

        const description = document.getElementById(targetId);

        if (!description) {
            return;
        }

        event.preventDefault();

        if (!link.dataset.originalText) {
            link.dataset.originalText = link.textContent.trim();
        }

        const isExpanded = description.classList.toggle('expanded');

        if (isExpanded) {
            link.textContent = 'Read Less';
        } else {
            link.textContent = link.dataset.originalText;
        }
    });

})();


// Navigation logic 


function toggleMob(id) {
    var el = document.getElementById(id);

    if (!el) {
        return;
    }

    el.classList.toggle('show');

    var trigger = el.previousElementSibling;

    if (trigger) {
        var icon = trigger.querySelector('i');

        if (icon) {
            if (icon.classList.contains('bi-chevron-down')) {
                icon.classList.replace('bi-chevron-down', 'bi-chevron-up');
            }
            else if (icon.classList.contains('bi-chevron-up')) {
                icon.classList.replace('bi-chevron-up', 'bi-chevron-down');
            }
            else if (icon.classList.contains('bi-plus')) {
                icon.classList.replace('bi-plus', 'bi-dash');
            }
            else if (icon.classList.contains('bi-dash')) {
                icon.classList.replace('bi-dash', 'bi-plus');
            }
        }
    }
}


function resetMobileNav() {
    document
        .querySelectorAll('#mMenu .mob-sub-list, #mMenu .mob-sub-container')
        .forEach(function (el) {
            el.classList.remove('show');
        });

    document
        .querySelectorAll('#mMenu i.bi-dash')
        .forEach(function (icon) {
            icon.classList.remove('bi-dash');
            icon.classList.add('bi-plus');
        });
}


// Mobile navigation
document.addEventListener("DOMContentLoaded", function () {
    var mMenu = document.getElementById('mMenu');

    if (mMenu) {
        mMenu.addEventListener('hidden.bs.offcanvas', function () {
            resetMobileNav();
        });
    }
});


// Desktop Mega Menu Logic
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll('.sidebar-btn').forEach(function (btn) {

        btn.addEventListener('mouseenter', function () {
            var panelId = this.getAttribute('data-panel');
            var container = this.closest('.etg-mega');

            if (!container) {
                return;
            }

            container
                .querySelectorAll('.sidebar-btn')
                .forEach(function (b) {
                    b.classList.remove('active');
                });

            container
                .querySelectorAll('.panel')
                .forEach(function (p) {
                    p.classList.remove('active');
                });

            this.classList.add('active');

            var panel = document.getElementById(panelId);

            if (panel) {
                panel.classList.add('active');
            }
        });

    });
});




/*google reviews widget custom js*/
function collapseCard(card) {
    const textEl = card.querySelector('.review-text');
    const btn = card.querySelector('.read-more-btn');

    if (!textEl || !btn) return;

    textEl.textContent = textEl.dataset.short;
    textEl.dataset.expanded = 'false';
    btn.textContent = 'Read more';
    btn.setAttribute('aria-expanded', 'false');
}

document.addEventListener('click', function (e) {
    const btn = e.target.closest('.read-more-btn');

    if (!btn) return;

    e.preventDefault();

    const card = btn.closest('.review-card');

    if (!card) return;

    const textEl = card.querySelector('.review-text');

    if (!textEl) return;

    const isExpanded = textEl.dataset.expanded === 'true';

    document.querySelectorAll('.review-card').forEach(function (otherCard) {
        if (otherCard !== card) {
            collapseCard(otherCard);
        }
    });

    if (isExpanded) {
        collapseCard(card);
    } else {
        textEl.textContent = textEl.dataset.full;
        textEl.dataset.expanded = 'true';
        btn.textContent = 'Read less';
        btn.setAttribute('aria-expanded', 'true');
    }
});

document.querySelectorAll('.review-card').forEach(function (card) {
    const textEl = card.querySelector('.review-text');

    if (!textEl) return;

    const fullText = textEl.dataset.full || textEl.textContent.trim();
    let shortText = textEl.dataset.short || fullText;

    if (shortText === fullText && fullText.length > 120) {
        shortText = fullText.substring(0, 120).trim() + '...';
        textEl.dataset.short = shortText;
    }

    textEl.textContent = shortText;
    textEl.dataset.expanded = 'false';
});


// Function for cookie consent validation (IIFE) and server side response 
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('consentForm');
        if (!form) {
            return;
        }

        var banner = form.closest('.cookie-consent-banner');

        form.addEventListener('submit', function (event) {
            event.preventDefault();

            var formData = new FormData(form);
            var submitter = event.submitter;
            if (submitter && submitter.name) {
                formData.set(submitter.name, submitter.value);
            }

            fetch(form.getAttribute('action'), {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('Consent submission failed with status ' + response.status);
                    }
                    if (banner) {
                        banner.remove();
                    }
                })
                .catch(function (error) {
                    console.error('Consent submission failed:', error);
                });
        });
    });
})();


// Tour package page tempalet custom js


document.addEventListener("DOMContentLoaded", function () {

    const expandAll = document.getElementById("expandAll");

    if (!expandAll) return;

    expandAll.addEventListener("click", function () {

        const accordions = document.querySelectorAll(
            "#itineraryAccordion .accordion-collapse"
        );

        const allExpanded = [...accordions].every(function (accordion) {
            return accordion.classList.contains("show");
        });

        // Temporarily remove Bootstrap's single-open restriction
        accordions.forEach(function (accordion) {
            accordion.removeAttribute("data-bs-parent");
        });

        accordions.forEach(function (accordion) {
            const collapse = bootstrap.Collapse.getOrCreateInstance(
                accordion,
                { toggle: false }
            );

            if (allExpanded) {
                collapse.hide();
            } else {
                collapse.show();
            }
        });

        expandAll.textContent = allExpanded
            ? "EXPAND ALL"
            : "COLLAPSE ALL";
    });

});






function initializeOfferCountdown() {
    document.querySelectorAll(".js-offer-countdown").forEach(timer => {
        const endDate = new Date(timer.dataset.end).getTime();
        if (Number.isNaN(endDate)) return;

        const daysElement = timer.querySelector('[data-countdown="days"]');
        const hoursElement = timer.querySelector('[data-countdown="hours"]');
        const minutesElement = timer.querySelector('[data-countdown="minutes"]');
        const secondsElement = timer.querySelector('[data-countdown="seconds"]');

        let interval = null;

        const updateCountdown = () => {
            const remaining = endDate - Date.now();

            if (remaining <= 0) {
                if (interval) clearInterval(interval);
                timer.innerHTML = "Offer ended";
                return;
            }

            if (daysElement) daysElement.textContent = String(Math.floor(remaining / 864e5)).padStart(2, "0");
            if (hoursElement) hoursElement.textContent = String(Math.floor(remaining / 36e5) % 24).padStart(2, "0");
            if (minutesElement) minutesElement.textContent = String(Math.floor(remaining / 6e4) % 60).padStart(2, "0");
            if (secondsElement) secondsElement.textContent = String(Math.floor(remaining / 1e3) % 60).padStart(2, "0");
        };

        updateCountdown();
        interval = setInterval(updateCountdown, 1000);
    });
}

document.addEventListener("DOMContentLoaded", initializeOfferCountdown);


document.addEventListener("DOMContentLoaded", function () {

    var nav = document.querySelector(".pkgd-tabs-bar__nav");
    var moreMenu = document.querySelector(".pkgd-more-menu");
    var moreBtn = document.querySelector(".pkgd-more-btn");
    var moreDropdown = document.querySelector(".pkgd-more-dropdown");

    if (!nav || !moreMenu || !moreBtn || !moreDropdown) return;

    var DESKTOP_BREAKPOINT = 1400;

    var mainItems = Array.from(
        nav.querySelectorAll(".nav-item:not(.pkgd-more-menu)")
    );
    var mainLinks = Array.from(
        nav.querySelectorAll(".nav-item:not(.pkgd-more-menu) .nav-link")
    );
    var dropdownLinks = Array.from(moreDropdown.querySelectorAll("a"));

    // Sections that exist on the page (taken from dropdown links, which list every tab)
    var sectionIds = dropdownLinks
        .map(function (a) { return (a.getAttribute("href") || "").replace("#", ""); })
        .filter(function (id) { return id && document.getElementById(id); });

    var currentId = null;
    var clickLock = false;
    var lockTimer = null;

    var isDesktop = function () {
        return window.innerWidth >= DESKTOP_BREAKPOINT;
    };

    var closeMore = function () {
        moreMenu.classList.remove("open");
        moreBtn.setAttribute("aria-expanded", "false");
    };

    /* ---------- Active state ---------- */

    var setActive = function (id) {
        if (!id) return;
        currentId = id;

        mainLinks.forEach(function (link) {
            link.classList.toggle("active", link.getAttribute("href") === "#" + id);
        });

        dropdownLinks.forEach(function (a) {
            a.classList.toggle("active", a.getAttribute("href") === "#" + id);
        });

        // Highlight the MORE button when the active tab is hidden inside it
        var activeItem = mainItems.find(function (item) {
            var link = item.querySelector(".nav-link");
            return link && link.getAttribute("href") === "#" + id;
        });
        var hiddenInMore = !activeItem || activeItem.style.display === "none";
        moreBtn.classList.toggle("active", hiddenInMore);
    };

    /* ---------- Smooth scroll ---------- */

    var getOffset = function () {
        var tabsBar = document.querySelector(".pkgd-tabs-bar");
        var tabsH = tabsBar ? tabsBar.offsetHeight : 0;
        var navH = window.innerWidth >= 768 ? 117 : 70;
        return navH + tabsH + 12;
    };

    var scrollToSection = function (id) {
        var target = document.getElementById(id);
        if (!target) return;

        // Lock the scroll-spy while the smooth scroll runs, so the
        // clicked tab does not flicker through the in-between sections
        clickLock = true;
        clearTimeout(lockTimer);
        lockTimer = setTimeout(function () { clickLock = false; }, 150);

        setActive(id);

        var top =
            target.getBoundingClientRect().top +
            window.pageYOffset -
            getOffset();

        window.scrollTo({ top: Math.max(0, top), behavior: "smooth" });
    };

    var handleLinkClick = function (e) {
        var href = this.getAttribute("href");
        if (!href || !href.startsWith("#")) return;

        e.preventDefault();
        e.stopPropagation();
        closeMore();
        scrollToSection(href.substring(1));
    };

    mainLinks.forEach(function (a) { a.addEventListener("click", handleLinkClick); });
    dropdownLinks.forEach(function (a) { a.addEventListener("click", handleLinkClick); });

    /* ---------- Scroll spy ---------- */

    var ticking = false;

    var onScroll = function () {
        if (clickLock) {
            // keep extending the lock until the smooth scroll has stopped
            clearTimeout(lockTimer);
            lockTimer = setTimeout(function () { clickLock = false; }, 150);
            return;
        }

        if (ticking) return;
        ticking = true;

        window.requestAnimationFrame(function () {
            ticking = false;

            var line = getOffset() + 20;
            var activeId = null;

            sectionIds.forEach(function (id) {
                var el = document.getElementById(id);
                if (el && el.getBoundingClientRect().top <= line) {
                    activeId = id;
                }
            });

            // At the very bottom of the page, pick the last section
            if (window.innerHeight + window.pageYOffset >= document.documentElement.scrollHeight - 2) {
                activeId = sectionIds[sectionIds.length - 1] || activeId;
            }

            if (!activeId) activeId = sectionIds[0];
            if (activeId && activeId !== currentId) setActive(activeId);
        });
    };

    window.addEventListener("scroll", onScroll, { passive: true });

    /* ---------- Overflow / MORE menu ---------- */

    var buildDropdownLinks = function (labelsToPush) {
        var anyVisible = false;

        dropdownLinks.forEach(function (a) {
            var show = labelsToPush.includes(a.getAttribute("data-label"));
            a.style.display = show ? "" : "none";
            if (show) anyVisible = true;
        });

        return anyVisible;
    };

    var updateDividers = function (visibleItems) {
        mainItems.forEach(function (item) { item.classList.remove("show-divider"); });
        visibleItems.forEach(function (item) { item.classList.add("show-divider"); });
    };

    var applyMobileLayout = function () {
        var labelsToPush = [];
        var visibleItems = [];

        var candidates = mainItems
            .filter(function (item) {
                return item.getAttribute("data-mobile-visible") === "true";
            })
            .slice(0, 3);

        mainItems.forEach(function (item) {
            item.style.display = candidates.includes(item) ? "" : "none";
        });
        moreMenu.style.display = "";

        var navWidth = nav.clientWidth;
        var gap = parseFloat(window.getComputedStyle(nav).columnGap) || 0;
        var used = moreMenu.offsetWidth;
        var overflowing = false;

        mainItems.forEach(function (item) {
            var label = item.getAttribute("data-label");

            if (!candidates.includes(item)) {
                if (label) labelsToPush.push(label);
                return;
            }

            var w = item.offsetWidth;

            // Not enough room? Send it (and everything after it) to MORE
            if (!overflowing && used + gap + w <= navWidth) {
                used += gap + w;
                visibleItems.push(item);
            } else {
                overflowing = true;
                item.style.display = "none";
                if (label) labelsToPush.push(label);
            }
        });

        var hasHidden = buildDropdownLinks(labelsToPush);
        moreMenu.style.display = hasHidden ? "" : "none";
        if (!hasHidden) closeMore();

        updateDividers(visibleItems);
    };

    var applyDesktopLayout = function () {
        mainItems.forEach(function (item) { item.style.display = ""; });
        moreMenu.style.display = "";

        var navWidth = nav.clientWidth;
        var gap = parseFloat(window.getComputedStyle(nav).columnGap) || 0;
        var moreWidth = moreMenu.offsetWidth;
        var widths = mainItems.map(function (item) { return item.offsetWidth; });

        var labelsToPush = [];
        var visibleItems = [];

        var totalWidth =
            widths.reduce(function (sum, w) { return sum + w; }, 0) +
            gap * Math.max(0, mainItems.length - 1);

        if (totalWidth <= navWidth) {
            visibleItems.push.apply(visibleItems, mainItems);
            moreMenu.style.display = "none";
            closeMore();
            buildDropdownLinks([]);
            updateDividers(visibleItems);
            return;
        }

        var used = moreWidth + gap;
        var overflowing = false;

        mainItems.forEach(function (item, i) {
            var label = item.getAttribute("data-label");

            if (!overflowing && used + widths[i] <= navWidth) {
                used += widths[i] + gap;
                visibleItems.push(item);
            } else {
                overflowing = true;
                item.style.display = "none";
                if (label) labelsToPush.push(label);
            }
        });

        var hasHidden = buildDropdownLinks(labelsToPush);
        moreMenu.style.display = hasHidden ? "" : "none";
        updateDividers(visibleItems);
    };

    var recalc = function () {
        if (isDesktop()) {
            applyDesktopLayout();
        } else {
            applyMobileLayout();
        }

        if (currentId) setActive(currentId);
    };

    /* ---------- MORE toggle ---------- */

    moreBtn.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();

        var isOpen = moreMenu.classList.toggle("open");
        moreBtn.setAttribute("aria-expanded", String(isOpen));
    });

    document.addEventListener("click", function (e) {
        if (!moreMenu.contains(e.target)) closeMore();
    });

    /* ---------- Init ---------- */

    var initial = nav.querySelector(".nav-link.active") || mainLinks[0];
    if (initial) {
        currentId = (initial.getAttribute("href") || "").replace("#", "") || null;
    }

    recalc();
    onScroll();

    var resizeTimer;
    window.addEventListener("resize", function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(recalc, 50);
    });

    // Re-measure once fonts/images have loaded (widths change after that)
    window.addEventListener("load", recalc);
    if (document.fonts && document.fonts.ready) {
        document.fonts.ready.then(recalc);
    }
});
/* ============================================================
   ADVANCED FILTERS ACCORDION & SHOW MORE
   ============================================================ */
function initAdvancedFilters() {
    // 1. Accordion Toggle
    const filterHeaders = document.querySelectorAll('.filter-header');
    filterHeaders.forEach(header => {
        // Prevent duplicate listener binding
        if (header.dataset.accordionBound === 'true') return;
        header.dataset.accordionBound = 'true';

        header.addEventListener('click', function() {
            const section = this.closest('.filter-section');
            if (!section) return;
            const content = section.querySelector('.filter-items');
            const icon = this.querySelector('.filter-accordion-icon');
            
            if (content) {
                // Toggle visibility
                if (content.style.display === 'none') {
                    content.style.display = 'block';
                    if (icon) {
                        icon.classList.remove('bi-chevron-down');
                        icon.classList.add('bi-chevron-up');
                    }
                } else {
                    content.style.display = 'none';
                    if (icon) {
                        icon.classList.remove('bi-chevron-up');
                        icon.classList.add('bi-chevron-down');
                    }
                }
            }
        });
    });

    // 2. Show More / Show Less functionality for checkbox groups
    const MAX_VISIBLE = 4;
    const filterSections = document.querySelectorAll('.filter-section');
    
    filterSections.forEach(section => {
        const filterItemsContainer = section.querySelector('.filter-items');
        if (!filterItemsContainer) return;
        
        const filterItems = filterItemsContainer.querySelectorAll('.filter-item');
        
        if (filterItems.length > MAX_VISIBLE) {
            // Check if showMoreBtn already exists
            if (filterItemsContainer.querySelector('.show-more-btn')) return;

            // Hide items beyond MAX_VISIBLE
            for (let i = MAX_VISIBLE; i < filterItems.length; i++) {
                filterItems[i].style.setProperty('display', 'none', 'important');
                filterItems[i].classList.add('extra-filter-item');
            }
            
            // Create "Show More" button
            const showMoreBtn = document.createElement('a');
            showMoreBtn.href = '#';
            showMoreBtn.className = 'text-pink text-decoration-underline small fw-semibold d-block mt-2 mb-2 show-more-btn';
            showMoreBtn.textContent = 'Show More';
            
            filterItemsContainer.appendChild(showMoreBtn);
            
            showMoreBtn.addEventListener('click', function(e) {
                e.preventDefault();
                const isExpanded = this.classList.contains('expanded');
                
                for (let i = MAX_VISIBLE; i < filterItems.length; i++) {
                    if (isExpanded) {
                        filterItems[i].style.setProperty('display', 'none', 'important');
                    } else {
                        filterItems[i].style.removeProperty('display');
                    }
                }
                
                if (isExpanded) {
                    this.textContent = 'Show More';
                    this.classList.remove('expanded');
                } else {
                    this.textContent = 'Show Less';
                    this.classList.add('expanded');
                }
            });
        }
    });
}

if (document.readyState === 'loading') {
    document.addEventListener("DOMContentLoaded", initAdvancedFilters);
} else {
    initAdvancedFilters();
}

    // Initialize custom Tom Select for search filters to fix z-index
    document.addEventListener("DOMContentLoaded", function () {
      document.querySelectorAll('select.search-filter-select').forEach(select => {
        // Prevent double-initialization and never run on wrapper divs or non-select elements
        if (!select || select.tagName !== "SELECT" || select.tomselect || select.closest(".ts-wrapper")) {
          return;
        }

        // Skip Algolia-managed dropdowns handled exclusively by dedicated page scripts
        if (select.id === "heroDestination" || select.id === "heroExperience" || select.id === "searchbox-destination" || select.id === "searchbox-experience") {
          return;
        }

        try {
          new TomSelect(select, {
            create: false,
            searchField: [],
            controlInput: null,
            wrapperClass: 'ts-wrapper search-filter-select-wrapper'
          });
        } catch (err) {
          console.error('TomSelect init failed for search-filter-select:', err);
        }
      });
    });

document.addEventListener("DOMContentLoaded", () => {

    const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');

    headings.forEach(heading => {
        if (!heading || typeof heading.innerText !== 'string' || !heading.innerText.trim()) return;
        if (heading.closest(".payment-lead-text, .no-titlecase")) return;
        // Process node by node to protect future child elements
        const capitalizeNodeText = (node) => {
            node.childNodes.forEach(child => {
                if (child.nodeType === Node.TEXT_NODE) {
                    child.textContent = child.textContent
                        .toLowerCase()
                        .replace(/\b\w/g, char => char.toUpperCase());
                } else if (child.nodeType === Node.ELEMENT_NODE) {
                    capitalizeNodeText(child);
                }
            });
        };
        capitalizeNodeText(heading);
    });
});