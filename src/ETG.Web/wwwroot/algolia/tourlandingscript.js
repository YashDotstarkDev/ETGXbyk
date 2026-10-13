(function () {
    const container = document.getElementById('algolia-landing');
    if (!container) return; 

    const APP_ID    = container.dataset.algoliaAppId;
    const API_KEY   = container.dataset.algoliaSearchKey;
    const INDEX     = container.dataset.algoliaIndex;

    if (!APP_ID || !API_KEY || !INDEX) return;

    // Helper to extract initial refinement parameters regardless of source page or routing style
    function getInitialParam(attribute) {
        const urlParams = new URLSearchParams(window.location.search);
        const attrLower = attribute.toLowerCase();
        const attrSingular = attrLower.replace(/s$/, '');

        // 1. InstantSearch index menu format: [index][menu][Attribute]
        const fromMenu = urlParams.get(INDEX + '_name_asc[menu][' + attribute + ']') ||
                         urlParams.get(INDEX + '[menu][' + attribute + ']');
        if (fromMenu) return decodeURIComponent(fromMenu);

        // 2. Direct query param format: ?destination=Alaska, ?destinations=Alaska, etc.
        const fromDirect = urlParams.get(attribute) ||
                           urlParams.get(attrLower) ||
                           urlParams.get(attrSingular);
        if (fromDirect) return decodeURIComponent(fromDirect);

        return null;
    }

    function getInitialQuery() {
        const urlParams = new URLSearchParams(window.location.search);
        const fromIndex = urlParams.get(INDEX + '_name_asc[query]') ||
                          urlParams.get(INDEX + '[query]') ||
                          urlParams.get('query') ||
                          urlParams.get('keyword') ||
                          urlParams.get('q');
        return fromIndex ? decodeURIComponent(fromIndex) : '';
    }

    const searchClient = algoliasearch(APP_ID, API_KEY);
    const searchIndex = INDEX + '_name_asc';

    // Pre-populate initialUiState so redirects from ANY page immediately activate filters in Algolia
    const initDest = getInitialParam('Destinations');
    const initExp = getInitialParam('Experiences');
    const initQuery = getInitialQuery();

    const initialUiState = {};
    initialUiState[searchIndex] = {};
    if (initQuery) {
        initialUiState[searchIndex].query = initQuery;
    }
    if ((initDest && initDest !== 'all') || (initExp && initExp !== 'all')) {
        initialUiState[searchIndex].menu = {};
        if (initDest && initDest !== 'all') {
            initialUiState[searchIndex].menu.Destinations = initDest;
        }
        if (initExp && initExp !== 'all') {
            initialUiState[searchIndex].menu.Experiences = initExp;
        }
    }

    const search = instantsearch({
        indexName: searchIndex,
        searchClient,
        routing: true,
        initialUiState: initialUiState
    });

    const customSearchBox = instantsearch.connectors.connectSearchBox(
        function(renderOptions, isFirstRender) {
            const { query, refine, widgetParams } = renderOptions;
            const input = document.querySelector(widgetParams.container);
            if (!input) return;
            if (isFirstRender) {
                input.addEventListener('input', function(e) { refine(e.target.value); });
            }
            input.value = query || '';
        }
    );

    const customDropdown = instantsearch.connectors.connectMenu(
        function(renderOptions, isFirstRender) {
            const { items, refine, widgetParams, instantsearchInstance } = renderOptions;
            const selectEl = document.querySelector(widgetParams.container);
            if (!selectEl) return;

            if (!selectEl.tomselect && window.TomSelect) {
                new TomSelect(selectEl, {
                    create: false,
                    controlInput: null,
                    wrapperClass: 'ts-wrapper search-filter-select-wrapper'
                });
            }

            const ts = selectEl.tomselect;

            if (isFirstRender) {
                const handleSelection = function(val) {
                    refine(val === 'all' ? '' : val);
                };
                selectEl.addEventListener('change', function(e) {
                    handleSelection(e.target.value);
                });
                if (ts) {
                    ts.on('change', function(val) {
                        handleSelection(val);
                    });
                }
            }

            // Determine active refinement even when items has 0 results
            let activeValue = null;
            const refinedItem = items.find(function(i) { return i.isRefined; });
            if (refinedItem) {
                activeValue = refinedItem.value;
            } else {
                const uiState = (instantsearchInstance || search).getUiState();
                const indexKey = search.indexName || searchIndex;
                const indexState = (uiState && uiState[indexKey]) || {};
                if (indexState.menu && indexState.menu[widgetParams.attribute]) {
                    activeValue = indexState.menu[widgetParams.attribute];
                } else {
                    activeValue = getInitialParam(widgetParams.attribute);
                }
            }

            if (ts) {
                if (!ts.options['all']) {
                    ts.addOption({ value: 'all', text: widgetParams.defaultOption || 'Select' });
                }

                items.forEach(function(item) {
                    if (!ts.options[item.value]) {
                        ts.addOption({ value: item.value, text: item.label });
                    }
                });

                if (activeValue && activeValue !== 'all') {
                    if (!ts.options[activeValue]) {
                        ts.addOption({ value: activeValue, text: activeValue });
                    }
                    ts.setValue(activeValue, true);
                } else {
                    ts.setValue('all', true);
                }
                ts.refreshOptions(false);
            } else {
                if (items.length > 0) {
                    selectEl.innerHTML = '<option value="all">' + (widgetParams.defaultOption || 'Select') + '</option>' +
                        items.map(function(item) {
                            return '<option value="' + item.value + '" ' + (item.isRefined ? 'selected' : '') + '>' + item.label + '</option>';
                        }).join('');
                }
                if (activeValue && activeValue !== 'all') {
                    selectEl.value = activeValue;
                } else {
                    selectEl.value = 'all';
                }
            }
        }
    );

    const customRangeSlider = instantsearch.connectors.connectRange(
        function(renderOptions, isFirstRender) {
            const { range, start, refine, widgetParams } = renderOptions;
            const containerEl = document.querySelector(widgetParams.container);
            
            let min = range ? range.min : widgetParams.defaultMin;
            let max = range ? range.max : widgetParams.defaultMax;
            
            if (min === max) {
                min = widgetParams.defaultMin;
                max = widgetParams.defaultMax;
            }

            const currentMin = start[0] !== -Infinity && start[0] !== undefined ? start[0] : min;
            const currentMax = start[1] !== Infinity && start[1] !== undefined ? start[1] : max;

            if (isFirstRender) {
                containerEl.innerHTML = 
                    '<div class="dual-slider-wrap">' +
                      '<div class="range-slider-container">' +
                        '<div class="slider-track"></div>' +
                        '<div class="slider-fill"></div>' +
                        '<input type="range" class="range-min">' +
                        '<input type="range" class="range-max">' +
                      '</div>' +
                      '<div class="d-flex justify-content-between text-muted small mt-2">' +
                        '<span class="val-min"></span>' +
                        '<span class="val-max"></span>' +
                      '</div>' +
                    '</div>';
                
                const minInput = containerEl.querySelector('.range-min');
                const maxInput = containerEl.querySelector('.range-max');
                
                const updateAndRefine = () => {
                    let vMin = parseInt(minInput.value);
                    let vMax = parseInt(maxInput.value);
                    if (vMin > vMax) {
                        const temp = vMin;
                        vMin = vMax;
                        vMax = temp;
                        minInput.value = vMin;
                        maxInput.value = vMax;
                    }
                    refine([vMin, vMax]);
                };

                minInput.addEventListener('change', updateAndRefine);
                maxInput.addEventListener('change', updateAndRefine);
                
                const liveVisualUpdate = () => {
                    let vMin = parseInt(minInput.value);
                    let vMax = parseInt(maxInput.value);
                    if (vMin > vMax) {
                        let temp = vMin; vMin = vMax; vMax = temp;
                    }
                    const trackWidth = maxInput.max - minInput.min;
                    const minPercent = ((vMin - minInput.min) / trackWidth) * 100;
                    const maxPercent = ((vMax - minInput.min) / trackWidth) * 100;
                    
                    const fill = containerEl.querySelector('.slider-fill');
                    if (fill) {
                        fill.style.left = minPercent + '%';
                        fill.style.width = (maxPercent - minPercent) + '%';
                    }
                    containerEl.querySelector('.val-min').textContent = (widgetParams.prefix || '') + vMin.toLocaleString() + (widgetParams.suffix || '');
                    containerEl.querySelector('.val-max').textContent = (widgetParams.prefix || '') + vMax.toLocaleString() + (widgetParams.suffix || '');
                };

                minInput.addEventListener('input', liveVisualUpdate);
                maxInput.addEventListener('input', liveVisualUpdate);
            }

            const minInput = containerEl.querySelector('.range-min');
            const maxInput = containerEl.querySelector('.range-max');
            const fill = containerEl.querySelector('.slider-fill');
            
            if (minInput && maxInput) {
                minInput.min = min; minInput.max = max;
                maxInput.min = min; maxInput.max = max;
                
                minInput.value = currentMin;
                maxInput.value = currentMax;
                
                const trackWidth = max - min;
                if (trackWidth > 0) {
                    const minPercent = ((currentMin - min) / trackWidth) * 100;
                    const maxPercent = ((currentMax - min) / trackWidth) * 100;
                    if (fill) {
                        fill.style.left = minPercent + '%';
                        fill.style.width = (maxPercent - minPercent) + '%';
                    }
                }
                
                containerEl.querySelector('.val-min').textContent = (widgetParams.prefix || '') + currentMin.toLocaleString() + (widgetParams.suffix || '');
                containerEl.querySelector('.val-max').textContent = (widgetParams.prefix || '') + currentMax.toLocaleString() + (widgetParams.suffix || '');
            }
        }
    );

    search.addWidgets([
        customSearchBox({ container: '#searchbox-keyword' }),
        customDropdown({ container: '#searchbox-destination', attribute: 'Destinations', defaultOption: 'Select destination', limit: 200, sortBy: ['name:asc'] }),
        customDropdown({ container: '#searchbox-experience', attribute: 'Experiences', defaultOption: 'Select experience', limit: 200, sortBy: ['name:asc'] }),

        instantsearch.widgets.stats({
            container: '#stats-container',
            templates: {
                text: function(options) {
                    return '<div class="text-dark search-result-count">Showing <span class="fw-bold">' + options.nbHits + '</span> results</div>';
                }
            }
        }),

        instantsearch.widgets.sortBy({
            container: '#sort-by',
            items: [
                { label: 'Alphabetical a to z', value: INDEX + '_name_asc' },
                { label: 'Alphabetical z to a', value: INDEX + '_name_desc' },
                { label: 'Price lowest to highest', value: INDEX + '_price_asc' },
                { label: 'Price highest to lowest', value: INDEX + '_price_desc' },
                { label: 'Oldest to newest', value: INDEX + '_date_asc' },
                { label: 'Newest to oldest', value: INDEX + '_date_desc' },
                { label: 'Duration short to long', value: INDEX + '_duration_asc' },
                { label: 'Duration long to short', value: INDEX + '_duration_desc' }
            ],
            cssClasses: { select: 'form-select sort-view-select shadow-sm' }
        }),

        instantsearch.widgets.hitsPerPage({
            container: '#hits-per-page',
            items: [
                { label: '30', value: 30, default: true },
                { label: '60', value: 60 }
            ],
            cssClasses: { select: 'form-select sort-view-select shadow-sm' }
        }),

        instantsearch.widgets.refinementList({
            container: '#refinement-travel-style',
            attribute: 'TravelStyles',
            limit: 4,
            showMore: true,
            showMoreLimit: 50,
            cssClasses: { 
                list: 'list-unstyled m-0', 
                item: 'm-0 p-0',
                showMore: 'text-pink text-decoration-underline small fw-semibold d-block mt-2 mb-2 show-more-btn border-0 bg-transparent p-0'
            },
            templates: {
                item: function(data) {
                    var formattedLabel = data.label.replace(/([A-Z])/g, ' $1').trim();
                    return '<div class="d-flex justify-content-between align-items-center mb-2 filter-item w-100" style="cursor:pointer;">' +
                           '<div class="form-check m-0">' +
                           '<input class="form-check-input custom-checkbox" type="checkbox" ' + (data.isRefined ? 'checked' : '') + ' style="pointer-events:none;">' +
                           '<label class="form-check-label text-muted" style="cursor:pointer;">' + formattedLabel + '</label>' +
                           '</div>' +
                           '<span class="text-muted small">' + data.count + '</span>' +
                           '</div>';
                },
                showMoreText: function(data) {
                    return data.isShowingMore ? 'Show Less' : 'Show More';
                }
            }
        }),

        instantsearch.widgets.refinementList({
            container: '#refinement-start-location',
            attribute: 'StartLocation',
            limit: 4,
            showMore: true,
            showMoreLimit: 50,
            cssClasses: { 
                list: 'list-unstyled m-0', 
                item: 'm-0 p-0',
                showMore: 'text-pink text-decoration-underline small fw-semibold d-block mt-2 mb-2 show-more-btn border-0 bg-transparent p-0'
            },
            templates: {
                item: function(data) {
                    var formattedLabel = data.label.replace(/([A-Z])/g, ' $1').trim();
                    return '<div class="d-flex justify-content-between align-items-center mb-2 filter-item w-100" style="cursor:pointer;">' +
                           '<div class="form-check m-0">' +
                           '<input class="form-check-input custom-checkbox" type="checkbox" ' + (data.isRefined ? 'checked' : '') + ' style="pointer-events:none;">' +
                           '<label class="form-check-label text-muted" style="cursor:pointer;">' + formattedLabel + '</label>' +
                           '</div>' +
                           '<span class="text-muted small">' + data.count + '</span>' +
                           '</div>';
                },
                showMoreText: function(data) {
                    return data.isShowingMore ? 'Show Less' : 'Show More';
                }
            }
        }),

        instantsearch.widgets.refinementList({
            container: '#refinement-end-location',
            attribute: 'EndLocation',
            limit: 4,
            showMore: true,
            showMoreLimit: 50,
            cssClasses: { 
                list: 'list-unstyled m-0', 
                item: 'm-0 p-0',
                showMore: 'text-pink text-decoration-underline small fw-semibold d-block mt-2 mb-2 show-more-btn border-0 bg-transparent p-0'
            },
            templates: {
                item: function(data) {
                    var formattedLabel = data.label.replace(/([A-Z])/g, ' $1').trim();
                    return '<div class="d-flex justify-content-between align-items-center mb-2 filter-item w-100" style="cursor:pointer;">' +
                           '<div class="form-check m-0">' +
                           '<input class="form-check-input custom-checkbox" type="checkbox" ' + (data.isRefined ? 'checked' : '') + ' style="pointer-events:none;">' +
                           '<label class="form-check-label text-muted" style="cursor:pointer;">' + formattedLabel + '</label>' +
                           '</div>' +
                           '<span class="text-muted small">' + data.count + '</span>' +
                           '</div>';
                },
                showMoreText: function(data) {
                    return data.isShowingMore ? 'Show Less' : 'Show More';
                }
            }
        }),

        customRangeSlider({ container: '#range-slider-price', attribute: 'Price', prefix: 'A$', defaultMin: 0, defaultMax: 20000 }),
        customRangeSlider({ container: '#range-slider-duration', attribute: 'Duration', suffix: ' Days', defaultMin: 1, defaultMax: 30 }),

        instantsearch.widgets.clearRefinements({
            container: '#clear-refinements-mobile', templates: { resetLabel: 'Clear Reset' },
            cssClasses: { button: 'btn btn-clear btn-outline-secondary w-100 h-100 py-2' }
        }),

        instantsearch.widgets.pagination({
            container: '#pagination',
            cssClasses: { list: 'pagination custom-pagination justify-content-center m-0', item: 'page-item', link: 'page-link border-0', selectedItem: 'active' }
        }),

       instantsearch.widgets.hits({
            container: '#hits',
            cssClasses: { list: 'row g-4 list-unstyled m-0 p-0', item: 'col-12 col-md-6 col-lg-4' },
            templates: {
                item: function(hit) {
                    var iconsMarkup = '';
                    
                    if (hit.PriceInclusions && Array.isArray(hit.PriceInclusions) && hit.PriceInclusions.length > 0) {
                        
                        var faMap = {
                            "flights": "fa-solid fa-plane",
                            "accommodation": "fa-solid fa-bed",
                            "meals": "fa-solid fa-utensils",
                            "transfer": "fa-solid fa-bus-simple"
                        };
                        hit.PriceInclusions.forEach(function(inclusion) {
                            var faClass = faMap[inclusion.toLowerCase().trim()];
                            if (faClass) {
                                if (iconsMarkup !== '') { iconsMarkup += '<span class="pink-v-line icon-divider"></span>'; }
                                iconsMarkup += '<div class="icon-wrapper"><i class="' + faClass + '" aria-hidden="true"></i><span class="tooltip-text">' + inclusion + '</span></div>';
                            }
                        });
                    } else {
                        // THIS WILL PROVE IF ALGOLIA IS SENDING EMPTY DATA!
                        iconsMarkup = '<span style="font-size:11px; color:red; font-weight:bold;">No Inclusions in Algolia!</span>';
                    }


                    var title = hit.TourName || hit.Title || '';
                    var desc = hit.Description || '';
                    var price = hit.Price ? hit.Price.toLocaleString() : '';
                    var duration = hit.Duration ? hit.Duration.toString().replace('Days', '').trim() : '';
                    var badgeText = hit.Destinations && hit.Destinations.length > 0 ? hit.Destinations[0].toUpperCase() : '';

                    return '<a href="' + (hit.Url || '#') + '" class="product-card-vertical h-100 text-decoration-none">' +
                             '<div class="img-wrap">' +
                               '<img loading="lazy" src="' + (hit.ImageUrl || '') + '" alt="' + title + '">' +
                               (badgeText ? '<span class="card-badge">' + badgeText + '</span>' : '') +
                             '</div>' +
                             '<div class="card-body-content flex-grow-1 d-flex flex-column">' +
                               '<h3 class="card-title text-dark">' + title + '</h3>' +
                               '<p class="card-desc three-line-desc text-muted">' + desc + '</p>' +
                               '<div class="tahiti-v2-price-block mt-auto">' +
                                 '<div class="original-price text-uppercase">' + duration + ' DAYS</div>' +
                                 (price ? '<div class="current-price text-pink">$' + price + ' <span style="font-size: 0.55em; margin-left: 2px; font-weight: 700;">AUD</span></div>' : '') +
                                 '<div class="price-note mb-3">Per person <span class="pink-v-line"></span> twin share</div>' +
                                 '<div class="card-icons text-pink">' + iconsMarkup + '</div>' +
                               '</div>' +
                             '</div>' +
                           '</a>';
                },
                empty: '<div class="col-12 text-center py-5 text-muted" style="font-size: 1.1rem;">No results found.</div>'
            }
        })
    ]);

    search.on('render', function() {
        var results = search.helper.lastResults;
        var nbHits = results ? results.nbHits : 0;
        var hitsPerPage = results ? results.hitsPerPage : 30;
        var pagContainer = document.getElementById('pagination');
        var navContainer = document.getElementById('pagination-nav');

        // Hide pagination when total results are less than or equal to current View limit (e.g. <= 30 or <= 60)
        if (nbHits <= hitsPerPage) {
            if (navContainer) navContainer.classList.add('d-none');
            if (pagContainer) {
                pagContainer.classList.add('d-none');
                pagContainer.classList.remove('d-flex');
            }
        } else {
            if (navContainer) navContainer.classList.remove('d-none');
            if (pagContainer) {
                pagContainer.classList.remove('d-none');
                pagContainer.classList.add('d-flex');
            }
        }
    });

    search.start();

    // Fetch master facet values for Destinations and Experiences so dropdown options are always populated
    const masterIndex = searchClient.initIndex(searchIndex);
    masterIndex.search('', { facets: ['Destinations', 'Experiences'], hitsPerPage: 0 })
        .then(function(res) {
            const destFacets = (res.facets && res.facets['Destinations']) || {};
            const expFacets = (res.facets && res.facets['Experiences']) || {};

            function populateMasterOptions(selectId, facets, defaultText, attribute) {
                const el = document.querySelector(selectId);
                if (!el) return;
                const ts = el.tomselect;
                const activeVal = getInitialParam(attribute);

                if (ts) {
                    if (!ts.options['all']) {
                        ts.addOption({ value: 'all', text: defaultText });
                    }
                    Object.keys(facets).sort().forEach(function(name) {
                        if (!ts.options[name]) {
                            ts.addOption({ value: name, text: name });
                        }
                    });
                    if (activeVal && activeVal !== 'all') {
                        if (!ts.options[activeVal]) {
                            ts.addOption({ value: activeVal, text: activeVal });
                        }
                        ts.setValue(activeVal, true);
                    }
                    ts.refreshOptions(false);
                }
            }

            populateMasterOptions('#searchbox-destination', destFacets, 'Select destination', 'Destinations');
            populateMasterOptions('#searchbox-experience', expFacets, 'Select experience', 'Experiences');
        })
        .catch(function(err) {
            console.warn('Master facet preload error:', err);
        });

    // FIND button form submit handler on search page
    const searchForm = document.querySelector('.search-bar-wrap form');
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const destVal = document.querySelector('#searchbox-destination')?.value;
            const expVal = document.querySelector('#searchbox-experience')?.value;
            const kwVal = document.querySelector('#searchbox-keyword')?.value;

            if (search.helper) {
                search.helper.setQuery(kwVal || '');
                if (destVal && destVal !== 'all') {
                    search.helper.setHierarchicalFacetBreadcrumb('Destinations', [destVal]);
                } else {
                    search.helper.setHierarchicalFacetBreadcrumb('Destinations', []);
                }
                if (expVal && expVal !== 'all') {
                    search.helper.setHierarchicalFacetBreadcrumb('Experiences', [expVal]);
                } else {
                    search.helper.setHierarchicalFacetBreadcrumb('Experiences', []);
                }
                search.helper.search();
            }
        });
    }
})();

