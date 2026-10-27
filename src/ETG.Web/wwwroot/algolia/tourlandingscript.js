(function () {
    const container = document.getElementById('algolia-landing');
    if (!container) return; 

    const APP_ID    = container.dataset.algoliaAppId;
    const API_KEY   = container.dataset.algoliaSearchKey;
    const INDEX     = container.dataset.algoliaIndex;

    if (!APP_ID || !API_KEY || !INDEX) return;

    // Track user interaction to drop from=homepage or from=404page once user touches search
    let userHasInteracted = false;
    ['click', 'change', 'input'].forEach(function(evt) {
        document.addEventListener(evt, function(e) {
            if (e.target.closest('#algolia-landing, .search-bar-wrap, #search-sidebar, #search-main')) {
                userHasInteracted = true;
            }
        }, true);
    });

    // Dynamic Currency from Navbar Dropdown (.conversion element)
    const convEl = document.querySelector('.conversion');
    const currencyCode = convEl ? (convEl.getAttribute('data-currency') || 'AUD') : 'AUD';
    const currencySymbol = convEl ? (convEl.getAttribute('data-symbol') || '$') : '$';
    const conversionRate = convEl ? (parseFloat(convEl.getAttribute('data-rate')) || 1) : 1;
    const appliesDiscount = convEl ? (convEl.getAttribute('data-discount') === 'true') : true;

    function convertPrice(audPrice) {
        if (!audPrice || isNaN(audPrice)) return 0;
        if (conversionRate === 1) return Math.round(audPrice);
        return Math.ceil(audPrice * conversionRate);
    }

    // Dynamic formatting helper: converts PascalCase / unspaced names to formatted display names with spaces
    // e.g., 'NewCaledonia' -> 'New Caledonia', 'CookIslands' -> 'Cook Islands', 'CityStays' -> 'City Stays'
    function formatDisplayName(str) {
        if (!str || str === 'all') return str;
        var clean = str.replace(/-[a-z0-9]{8}$/i, '');
        clean = clean.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
        clean = clean.replace(/\s*&\s*/g, ' & ');
        return clean.replace(/\s+/g, ' ').trim();
    }

    // Helper to check if a list contains a value (case-insensitive and format-agnostic)
    function listContains(list, val) {
        if (!val || !list) return false;
        var fVal = formatDisplayName(val).toLowerCase();
        var uVal = val.replace(/\s+/g, '').toLowerCase();
        return list.some(function(item) {
            var fItem = formatDisplayName(item).toLowerCase();
            var uItem = item.replace(/\s+/g, '').toLowerCase();
            return fItem === fVal || uItem === uVal;
        });
    }

    // Dynamically expands any facet filter to include both spaced and unspaced variants
    // e.g., "Destinations:New Caledonia" expands to ["Destinations:New Caledonia", "Destinations:NewCaledonia"]
    function expandFilterItem(str) {
        if (typeof str !== 'string') return [str];
        var colonIdx = str.indexOf(':');
        if (colonIdx === -1) return [str];
        var attr = str.slice(0, colonIdx);
        var val = str.slice(colonIdx + 1);

        var variants = [val];
        var unspaced = val.replace(/\s+/g, '');
        if (unspaced && variants.indexOf(unspaced) === -1) {
            variants.push(unspaced);
        }
        var formatted = formatDisplayName(val);
        if (formatted && variants.indexOf(formatted) === -1) {
            variants.push(formatted);
        }

        return variants.map(function(v) {
            return attr + ':' + v;
        });
    }

    function expandFacetFilters(facetFilters) {
        if (!facetFilters) return facetFilters;
        if (Array.isArray(facetFilters)) {
            return facetFilters.map(function(item) {
                if (Array.isArray(item)) {
                    var inner = [];
                    item.forEach(function(sub) {
                        expandFilterItem(sub).forEach(function(v) {
                            if (inner.indexOf(v) === -1) inner.push(v);
                        });
                    });
                    return inner;
                } else if (typeof item === 'string') {
                    return expandFilterItem(item);
                }
                return item;
            });
        }
        return facetFilters;
    }

    function transformParams(params) {
        if (!params) return params;
        if (typeof params === 'object') {
            var copy = Object.assign({}, params);
            if (copy.facetFilters) {
                copy.facetFilters = expandFacetFilters(copy.facetFilters);
            }
            return copy;
        }
        if (typeof params === 'string') {
            var urlParams = new URLSearchParams(params);
            if (urlParams.has('facetFilters')) {
                try {
                    var parsed = JSON.parse(urlParams.get('facetFilters'));
                    var expanded = expandFacetFilters(parsed);
                    urlParams.set('facetFilters', JSON.stringify(expanded));
                    return urlParams.toString();
                } catch (e) {
                    return params;
                }
            }
        }
        return params;
    }

    // Helper to robustly extract initial refinement parameters matching Kentico 13 URL schema
    // Accurately decodes ampersands and special characters (e.g., "Italy & more")
    function getInitialParam(attribute) {
        const urlParams = new URLSearchParams(window.location.search);
        const attrLower = attribute.toLowerCase();
        const k13Key = attrLower === 'destinations' ? 'destination' : (attrLower === 'experiences' ? 'experience' : attribute);

        for (const [k, v] of urlParams.entries()) {
            const decKey = decodeURIComponent(k).toLowerCase();
            if (decKey.includes('[refinementlist][' + k13Key.toLowerCase() + ']') ||
                (attrLower === 'experiences' && (decKey.includes('[refinementlist][experiences]') || decKey.includes('[refinementlist][startcity]'))) ||
                decKey.includes('[menu][' + attrLower + ']')) {
                return v;
            }
        }
        return urlParams.get(attribute) || urlParams.get(attrLower) || urlParams.get(k13Key) || null;
    }

    function getInitialQuery() {
        const urlParams = new URLSearchParams(window.location.search);
        for (const [k, v] of urlParams.entries()) {
            const decKey = decodeURIComponent(k).toLowerCase();
            if (decKey.includes('[query]') || decKey === 'query' || decKey === 'keyword' || decKey === 'q') {
                return v;
            }
        }
        return '';
    }

    function getInitialHitsPerPage() {
        const urlParams = new URLSearchParams(window.location.search);
        for (const [k, v] of urlParams.entries()) {
            const decKey = decodeURIComponent(k).toLowerCase();
            if (decKey.includes('hitsperpage')) {
                return parseInt(v, 10) || 30;
            }
        }
        return 30;
    }

    function getInitialOnSale() {
        const urlParams = new URLSearchParams(window.location.search);
        for (const [k, v] of urlParams.entries()) {
            const decKey = decodeURIComponent(k).toLowerCase();
            if (decKey.includes('options.onsale') || decKey === 'onsale') {
                return v === 'true';
            }
        }
        return false;
    }

    function getInitialTravelStyles() {
        const urlParams = new URLSearchParams(window.location.search);
        const styles = [];
        for (const [k, v] of urlParams.entries()) {
            const decKey = decodeURIComponent(k).toLowerCase();
            if (decKey.includes('[refinementlist][travelstyles]')) {
                styles.push(v);
            }
        }
        return styles;
    }

    // Initialize Algolia client with automatic facet filter expansion for spaced & unspaced compatibility
    const rawSearchClient = algoliasearch(APP_ID, API_KEY);
    const searchClient = {
        search: function(requests) {
            var transformed = (requests || []).map(function(req) {
                var copy = Object.assign({}, req);
                if (copy.params) {
                    copy.params = transformParams(copy.params);
                }
                return copy;
            });
            return rawSearchClient.search(transformed);
        },
        searchForFacetValues: function(requests) {
            if (typeof rawSearchClient.searchForFacetValues === 'function') {
                var transformed = (requests || []).map(function(req) {
                    var copy = Object.assign({}, req);
                    if (copy.params) {
                        copy.params = transformParams(copy.params);
                    }
                    return copy;
                });
                return rawSearchClient.searchForFacetValues(transformed);
            }
            return Promise.resolve([]);
        },
        initIndex: function(name) {
            var idx = rawSearchClient.initIndex(name);
            var originalSearch = idx.search.bind(idx);
            idx.search = function(query, args) {
                var copyArgs = Object.assign({}, args);
                if (copyArgs && copyArgs.facetFilters) {
                    copyArgs.facetFilters = expandFacetFilters(copyArgs.facetFilters);
                }
                return originalSearch(query, copyArgs);
            };
            return idx;
        }
    };
    const searchIndex = INDEX;
    const masterIndex = searchClient.initIndex(INDEX);

    // Pre-populate initialUiState ONLY ONCE on initial load
    const initDest = getInitialParam('Destinations');
    const initExp = getInitialParam('Experiences');
    const initQuery = getInitialQuery();
    const initHitsPerPage = getInitialHitsPerPage();
    const initOnSale = getInitialOnSale();
    const initTravelStyles = getInitialTravelStyles();

    const initialUiState = {};
    initialUiState[searchIndex] = {};
    if (initQuery) {
        initialUiState[searchIndex].query = initQuery;
    }
    if (initHitsPerPage !== 30) {
        initialUiState[searchIndex].hitsPerPage = initHitsPerPage;
    }
    if ((initDest && initDest !== 'all') || (initExp && initExp !== 'all')) {
        initialUiState[searchIndex].menu = {};
        if (initDest && initDest !== 'all') {
            initialUiState[searchIndex].menu.Destinations = formatDisplayName(initDest);
        }
        if (initExp && initExp !== 'all') {
            initialUiState[searchIndex].menu.Experiences = formatDisplayName(initExp);
        }
    }
    if (initOnSale || (initTravelStyles && initTravelStyles.length)) {
        initialUiState[searchIndex].refinementList = {};
        if (initOnSale) {
            initialUiState[searchIndex].refinementList.MoreOptions = ['On Sale'];
        }
        if (initTravelStyles && initTravelStyles.length) {
            initialUiState[searchIndex].refinementList.TravelStyles = initTravelStyles;
        }
    }

    let masterDestinations = [];
    let masterExperiences = [];
    const destExpCache = {};
    const expDestCache = {};
    let isSyncingDropdowns = false;

    function fetchExperiencesForDestination(dest, callback) {
        if (!dest || dest === 'all') {
            callback(masterExperiences);
            return;
        }
        var formattedDest = formatDisplayName(dest);
        if (destExpCache[formattedDest]) {
            callback(destExpCache[formattedDest]);
            return;
        }
        masterIndex.search('', { facetFilters: [['Destinations:' + formattedDest]], facets: ['Experiences'], hitsPerPage: 0 })
            .then(function(res) {
                const list = Object.keys((res.facets && res.facets['Experiences']) || {}).sort();
                destExpCache[formattedDest] = list;
                callback(list);
            })
            .catch(function(err) {
                console.warn('Error fetching experiences for ' + dest, err);
                callback(masterExperiences);
            });
    }

    function fetchDestinationsForExperience(exp, callback) {
        if (!exp || exp === 'all') {
            callback(masterDestinations);
            return;
        }
        var formattedExp = formatDisplayName(exp);
        if (expDestCache[formattedExp]) {
            callback(expDestCache[formattedExp]);
            return;
        }
        masterIndex.search('', { facetFilters: [['Experiences:' + formattedExp]], facets: ['Destinations'], hitsPerPage: 0 })
            .then(function(res) {
                const list = Object.keys((res.facets && res.facets['Destinations']) || {}).sort();
                expDestCache[formattedExp] = list;
                callback(list);
            })
            .catch(function(err) {
                console.warn('Error fetching destinations for ' + exp, err);
                callback(masterDestinations);
            });
    }

    function updateTomSelect(selectId, list, activeVal, defaultText) {
        const el = document.querySelector(selectId);
        if (!el || !el.tomselect) return;
        const ts = el.tomselect;

        const newOptions = {};
        newOptions['all'] = { value: 'all', text: defaultText };
        (list || []).forEach(function(item) {
            var formatted = formatDisplayName(item);
            newOptions[formatted] = { value: formatted, text: formatted };
        });

        const curKeys = Object.keys(ts.options).filter(function(k) { return k !== 'all'; }).sort().join('|');
        const newKeys = Object.keys(newOptions).filter(function(k) { return k !== 'all'; }).sort().join('|');
        if (curKeys !== newKeys) {
            isSyncingDropdowns = true;
            ts.clear(true);
            ts.clearOptions();
            Object.keys(newOptions).forEach(function(k) {
                ts.addOption(newOptions[k]);
            });
            ts.refreshOptions(false);
            isSyncingDropdowns = false;
        }
        const formattedActive = formatDisplayName(activeVal);
        const targetVal = (formattedActive && newOptions[formattedActive]) ? formattedActive : 'all';
        if (ts.getValue() !== targetVal) {
            isSyncingDropdowns = true;
            ts.setValue(targetVal, true);
            isSyncingDropdowns = false;
        }
    }

    const search = instantsearch({
        indexName: searchIndex,
        searchClient,
        routing: {
            router: instantsearch.routers.history({
                cleanUrlOnDispose: false
            }),
            stateMapping: {
                stateToRoute: function(uiState) {
                    const indexUiState = uiState[searchIndex] || {};
                    const route = {};
                    const indexRoute = {};

                    if (indexUiState.query) {
                        indexRoute.query = indexUiState.query;
                    }

                    const refinementList = {};
                    if (indexUiState.menu && indexUiState.menu.Destinations) {
                        refinementList.destination = [formatDisplayName(indexUiState.menu.Destinations)];
                    }
                    if (indexUiState.menu && indexUiState.menu.Experiences) {
                        refinementList.experience = [formatDisplayName(indexUiState.menu.Experiences)];
                    }
                    if (indexUiState.refinementList) {
                        if (indexUiState.refinementList.TravelStyles && indexUiState.refinementList.TravelStyles.length) {
                            refinementList.travelStyles = indexUiState.refinementList.TravelStyles;
                        }
                        if (indexUiState.refinementList.StartLocation && indexUiState.refinementList.StartLocation.length) {
                            refinementList.startLocation = indexUiState.refinementList.StartLocation;
                        }
                        if (indexUiState.refinementList.EndLocation && indexUiState.refinementList.EndLocation.length) {
                            refinementList.endCity = indexUiState.refinementList.EndLocation;
                        }
                    }
                    if (Object.keys(refinementList).length > 0) {
                        indexRoute.refinementList = refinementList;
                    }

                    if (indexUiState.refinementList && indexUiState.refinementList.MoreOptions) {
                        const more = indexUiState.refinementList.MoreOptions;
                        const toggle = {};
                        if (more.indexOf('On Sale') !== -1) toggle['options.onSale'] = 'true';
                        if (more.indexOf('Freedom of choice') !== -1) toggle['options.freedomOfChoice'] = 'true';
                        if (more.indexOf('Peace of mind') !== -1) toggle['options.peaceOfMind'] = 'true';
                        if (more.indexOf('Sustainable Travel') !== -1) toggle['options.safeTravels'] = 'true';
                        if (Object.keys(toggle).length > 0) {
                            indexRoute.toggle = toggle;
                        }
                    }

                    if (indexUiState.range) {
                        const range = {};
                        if (indexUiState.range.Price) {
                            range.price = indexUiState.range.Price;
                        }
                        if (indexUiState.range.Duration) {
                            range.duration = indexUiState.range.Duration;
                        }
                        if (Object.keys(range).length > 0) {
                            indexRoute.range = range;
                        }
                    }

                    if (indexUiState.sortBy && indexUiState.sortBy !== searchIndex) {
                        indexRoute.sortBy = indexUiState.sortBy;
                    }

                    if (indexUiState.page && indexUiState.page > 1) {
                        indexRoute.page = indexUiState.page;
                    }

                    if (indexUiState.hitsPerPage && indexUiState.hitsPerPage !== 30) {
                        indexRoute.hitsPerPage = indexUiState.hitsPerPage;
                    }

                    if (Object.keys(indexRoute).length > 0) {
                        route[searchIndex] = indexRoute;
                    }

                    // Keep from=homepage or from=404page ONLY on initial load before any user interaction
                    if (!userHasInteracted) {
                        const currentParams = new URLSearchParams(window.location.search);
                        if (currentParams.has('from')) {
                            route.from = currentParams.get('from');
                        }
                    }

                    return route;
                },
                routeToState: function(routeState) {
                    if (!routeState) return {};
                    const indexRoute = routeState[searchIndex] || {};
                    const indexUiState = {};

                    const q = indexRoute.query || routeState.query || routeState.keyword || routeState.q;
                    if (q) {
                        indexUiState.query = q;
                    }

                    indexUiState.menu = {};
                    let dest = null;
                    if (indexRoute.refinementList) {
                        dest = indexRoute.refinementList.destination || indexRoute.refinementList.Destinations;
                    }
                    if (!dest) {
                        dest = routeState.destination || routeState.destinations || routeState.Destinations;
                    }
                    if (dest) {
                        const destVal = Array.isArray(dest) ? dest[0] : dest;
                        indexUiState.menu.Destinations = formatDisplayName(destVal);
                    }

                    let exp = null;
                    if (indexRoute.refinementList) {
                        exp = indexRoute.refinementList.experience || indexRoute.refinementList.experiences || indexRoute.refinementList.Experiences || indexRoute.refinementList.startCity;
                    }
                    if (!exp) {
                        exp = routeState.experience || routeState.experiences || routeState.Experiences || routeState.startCity;
                    }
                    if (exp) {
                        const expVal = Array.isArray(exp) ? exp[0] : exp;
                        indexUiState.menu.Experiences = formatDisplayName(expVal);
                    }

                    const hpp = indexRoute.hitsPerPage || routeState.hitsPerPage;
                    if (hpp) {
                        indexUiState.hitsPerPage = parseInt(hpp, 10);
                    }

                    indexUiState.refinementList = {};
                    if (indexRoute.refinementList) {
                        const ts = indexRoute.refinementList.travelStyles || indexRoute.refinementList.TravelStyles;
                        if (ts && ts.length) {
                            indexUiState.refinementList.TravelStyles = Array.isArray(ts) ? ts : [ts];
                        }

                        const sl = indexRoute.refinementList.startLocation || indexRoute.refinementList.StartLocation;
                        if (sl && sl.length) {
                            indexUiState.refinementList.StartLocation = Array.isArray(sl) ? sl : [sl];
                        }

                        const el = indexRoute.refinementList.endCity || indexRoute.refinementList.EndLocation;
                        if (el && el.length) {
                            indexUiState.refinementList.EndLocation = Array.isArray(el) ? el : [el];
                        }
                    }
                    if (routeState.travelStyles || routeState.TravelStyles) {
                        const ts = routeState.travelStyles || routeState.TravelStyles;
                        indexUiState.refinementList.TravelStyles = Array.isArray(ts) ? ts : [ts];
                    }
                    if (routeState.startLocation || routeState.StartLocation) {
                        const sl = routeState.startLocation || routeState.StartLocation;
                        indexUiState.refinementList.StartLocation = Array.isArray(sl) ? sl : [sl];
                    }
                    if (routeState.endCity || routeState.EndLocation) {
                        const el = routeState.endCity || routeState.EndLocation;
                        indexUiState.refinementList.EndLocation = Array.isArray(el) ? el : [el];
                    }

                    const toggle = indexRoute.toggle || routeState.toggle;
                    if (toggle) {
                        const moreOptions = [];
                        if (toggle['options.onSale']) moreOptions.push('On Sale');
                        if (toggle['options.freedomOfChoice']) moreOptions.push('Freedom of choice');
                        if (toggle['options.peaceOfMind']) moreOptions.push('Peace of mind');
                        if (toggle['options.safeTravels']) moreOptions.push('Sustainable Travel');
                        if (moreOptions.length > 0) {
                            indexUiState.refinementList.MoreOptions = moreOptions;
                        }
                    }
                    if (routeState.onsale === 'true' || routeState.onSale === 'true') {
                        indexUiState.refinementList.MoreOptions = indexUiState.refinementList.MoreOptions || [];
                        if (!indexUiState.refinementList.MoreOptions.includes('On Sale')) {
                            indexUiState.refinementList.MoreOptions.push('On Sale');
                        }
                    }

                    const page = indexRoute.page || routeState.page;
                    if (page) {
                        indexUiState.page = parseInt(page, 10);
                    }

                    const range = indexRoute.range || routeState.range;
                    if (range) {
                        indexUiState.range = {};
                        const p = range.price || range.Price;
                        if (p) indexUiState.range.Price = p;
                        const d = range.duration || range.Duration;
                        if (d) indexUiState.range.Duration = d;
                    } else {
                        if (routeState.price) {
                            indexUiState.range = indexUiState.range || {};
                            indexUiState.range.Price = routeState.price;
                        }
                        if (routeState.duration) {
                            indexUiState.range = indexUiState.range || {};
                            indexUiState.range.Duration = routeState.duration;
                        }
                    }

                    const sb = indexRoute.sortBy || routeState.sortBy;
                    if (sb) {
                        indexUiState.sortBy = sb;
                    }

                    return {
                        [searchIndex]: indexUiState
                    };
                }
            }
        },
        initialUiState: initialUiState
    });

    const customSearchBox = instantsearch.connectors.connectSearchBox(
        function(renderOptions, isFirstRender) {
            const { query, refine, widgetParams } = renderOptions;
            const input = document.querySelector(widgetParams.container);
            if (!input) return;

            if (isFirstRender) {
                let debounceTimer = null;
                input.addEventListener('input', function(e) {
                    userHasInteracted = true;
                    const val = e.target.value;
                    clearTimeout(debounceTimer);
                    debounceTimer = setTimeout(function() {
                        refine(val);
                    }, 300);
                });
                input.addEventListener('keydown', function(e) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        userHasInteracted = true;
                        clearTimeout(debounceTimer);
                        refine(input.value);
                    }
                });
            }

            if (input.value !== query && document.activeElement !== input) {
                input.value = query || '';
            }
        }
    );

    const customDropdown = instantsearch.connectors.connectMenu(
        function(renderOptions, isFirstRender) {
            const { items, refine, widgetParams, helper } = renderOptions;
            const selectEl = document.querySelector(widgetParams.container);
            if (!selectEl) return;

            if (!selectEl.tomselect && window.TomSelect) {
                new TomSelect(selectEl, {
                    create: false,
                    controlInput: null,
                    maxOptions: 1000,
                    wrapperClass: 'ts-wrapper search-filter-select-wrapper'
                });
            }

            const ts = selectEl.tomselect;

            let activeValue = null;
            const refinedItem = items.find(function(i) { return i.isRefined; });
            if (refinedItem) {
                activeValue = refinedItem.value;
            } else if (helper && typeof helper.getHierarchicalFacetBreadcrumb === 'function') {
                const breadcrumb = helper.getHierarchicalFacetBreadcrumb(widgetParams.attribute);
                if (breadcrumb && breadcrumb.length > 0) {
                    activeValue = breadcrumb[0];
                }
            }
            if (!activeValue) {
                const uiState = (renderOptions.instantSearchInstance || search).getUiState();
                const indexKey = search.indexName || searchIndex;
                const indexState = (uiState && uiState[indexKey]) || {};
                if (indexState.menu && indexState.menu[widgetParams.attribute]) {
                    activeValue = indexState.menu[widgetParams.attribute];
                }
            }

            if (isFirstRender) {
                const handleSelection = function(val) {
                    if (isSyncingDropdowns) return;
                    userHasInteracted = true;

                    var formattedVal = formatDisplayName(val);

                    // Immediately update the OTHER dropdown dynamically so invalid options never appear
                    if (widgetParams.attribute === 'Destinations') {
                        fetchExperiencesForDestination(formattedVal, function(expList) {
                            const curExpEl = document.querySelector('#searchbox-experience');
                            const curExp = curExpEl && curExpEl.tomselect ? curExpEl.tomselect.getValue() : null;
                            const targetExp = (curExp && listContains(expList, curExp)) ? curExp : 'all';
                            updateTomSelect('#searchbox-experience', expList, targetExp, 'Select experience');
                        });
                    } else if (widgetParams.attribute === 'Experiences') {
                        fetchDestinationsForExperience(formattedVal, function(destList) {
                            const curDestEl = document.querySelector('#searchbox-destination');
                            const curDest = curDestEl && curDestEl.tomselect ? curDestEl.tomselect.getValue() : null;
                            const targetDest = (curDest && listContains(destList, curDest)) ? curDest : 'all';
                            updateTomSelect('#searchbox-destination', destList, targetDest, 'Select destination');
                        });
                    }

                    if (val === 'all' || !val) {
                        const current = (helper && helper.getHierarchicalFacetBreadcrumb(widgetParams.attribute)[0]) || activeValue;
                        if (current) {
                            refine(current);
                        } else {
                            refine('');
                        }
                    } else {
                        refine(formattedVal);
                    }
                };

                if (ts) {
                    ts.on('change', function(val) {
                        handleSelection(val);
                    });
                } else {
                    selectEl.addEventListener('change', function(e) {
                        handleSelection(e.target.value);
                    });
                }
            }

            const formattedTarget = formatDisplayName(activeValue);
            const targetVal = formattedTarget || 'all';

            if (ts) {
                if (targetVal !== 'all' && !ts.options[targetVal]) {
                    ts.addOption({ value: targetVal, text: targetVal });
                }
                if (ts.getValue() !== targetVal) {
                    isSyncingDropdowns = true;
                    ts.setValue(targetVal, true);
                    isSyncingDropdowns = false;
                }
            } else {
                selectEl.value = targetVal;
            }
        }
    );

    const customRangeSlider = instantsearch.connectors.connectRange(
        function(renderOptions, isFirstRender) {
            const { range, start, refine, widgetParams } = renderOptions;
            const containerEl = document.querySelector(widgetParams.container);
            if (!containerEl) return;
            
            let min = (range && typeof range.min === 'number' && isFinite(range.min)) ? range.min : widgetParams.defaultMin;
            let max = (range && typeof range.max === 'number' && isFinite(range.max)) ? range.max : widgetParams.defaultMax;
            
            if (min >= max) {
                max = min + 1;
            }

            let currentMin = (start && start[0] !== -Infinity && start[0] !== undefined && isFinite(start[0])) ? start[0] : min;
            let currentMax = (start && start[1] !== Infinity && start[1] !== undefined && isFinite(start[1])) ? start[1] : max;

            currentMin = Math.max(min, Math.min(currentMin, max));
            currentMax = Math.min(max, Math.max(currentMax, min));
            if (currentMin > currentMax) {
                currentMin = min;
                currentMax = max;
            }

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
                const fill = containerEl.querySelector('.slider-fill');
                const minLabel = containerEl.querySelector('.val-min');
                const maxLabel = containerEl.querySelector('.val-max');

                function updateTrackAndLabels() {
                    const cMin = parseInt(minInput.value, 10);
                    const cMax = parseInt(maxInput.value, 10);
                    const rMin = parseInt(minInput.min, 10);
                    const rMax = parseInt(minInput.max, 10);
                    const span = (rMax - rMin) || 1;

                    const p1 = Math.max(0, Math.min(100, ((cMin - rMin) / span) * 100));
                    const p2 = Math.max(0, Math.min(100, ((cMax - rMin) / span) * 100));

                    fill.style.left = p1 + '%';
                    fill.style.width = (p2 - p1) + '%';

                    const prefix = widgetParams.prefix || '';
                    const suffix = widgetParams.suffix || '';
                    minLabel.textContent = prefix + cMin.toLocaleString() + suffix;
                    maxLabel.textContent = prefix + cMax.toLocaleString() + suffix;
                }

                let debounceTimer = null;
                function onInputChange(e) {
                    userHasInteracted = true;
                    let vMin = parseInt(minInput.value, 10);
                    let vMax = parseInt(maxInput.value, 10);

                    if (vMin > vMax) {
                        if (e && e.target === minInput) {
                            minInput.value = vMax;
                            vMin = vMax;
                        } else {
                            maxInput.value = vMin;
                            vMax = vMin;
                        }
                    }

                    updateTrackAndLabels();

                    clearTimeout(debounceTimer);
                    debounceTimer = setTimeout(function() {
                        const rMin = parseInt(minInput.min, 10);
                        const rMax = parseInt(minInput.max, 10);
                        const finalMin = vMin <= rMin ? undefined : vMin;
                        const finalMax = vMax >= rMax ? undefined : vMax;
                        refine([finalMin, finalMax]);
                    }, 250);
                }

                minInput.addEventListener('input', onInputChange);
                maxInput.addEventListener('input', onInputChange);

                containerEl._controls = {
                    minInput: minInput,
                    maxInput: maxInput,
                    updateTrackAndLabels: updateTrackAndLabels
                };
            }

            if (containerEl._controls) {
                const { minInput, maxInput, updateTrackAndLabels } = containerEl._controls;
                minInput.min = min;
                minInput.max = max;
                maxInput.min = min;
                maxInput.max = max;

                minInput.value = Math.max(min, Math.min(currentMin, max));
                maxInput.value = Math.min(max, Math.max(currentMax, min));

                updateTrackAndLabels();
            }
        }
    );

    search.addWidgets([
        instantsearch.widgets.configure({
            disableTypoToleranceOnAttributes: ['TourCode'],
            maxValuesPerFacet: 1000
        }),
        customSearchBox({ container: '#searchbox-keyword' }),
        customDropdown({ container: '#searchbox-destination', attribute: 'Destinations', defaultOption: 'Select destination', limit: 1000, sortBy: ['name:asc'] }),
        customDropdown({ container: '#searchbox-experience', attribute: 'Experiences', defaultOption: 'Select experience', limit: 1000, sortBy: ['name:asc'] }),

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
                { label: 'Price lowest to highest', value: INDEX },
                { label: 'Price highest to lowest', value: INDEX + '_price_desc' },
                { label: 'Oldest to newest', value: INDEX + '_date_asc' },
                { label: 'Newest to oldest', value: INDEX + '_date_desc' },
                { label: 'Alphabetical a to z', value: INDEX + '_alpha_asc' },
                { label: 'Alphabetical z to a', value: INDEX + '_alpha_desc' },
                { label: 'Duration lowest to highest', value: INDEX + '_duration_asc' },
                { label: 'Duration highest to lowest', value: INDEX + '_duration_desc' }
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
            sortBy: ['isRefined', 'name:asc'],
            showMore: true,
            showMoreLimit: 1000,
            cssClasses: { 
                list: 'list-unstyled m-0', 
                item: 'm-0 p-0',
                showMore: 'text-pink text-decoration-underline small fw-semibold mt-2 mb-2 show-more-btn border-0 bg-transparent p-0',
                disabledShowMore: 'd-none'
            },
            templates: {
                item: function(data) {
                    var formattedLabel = formatDisplayName(data.label);
                    return '<div class="d-flex justify-content-between align-items-center mb-2 filter-item w-100 gap-2" style="cursor:pointer;">' +
                           '<div class="form-check m-0 me-2 flex-grow-1">' +
                           '<input class="form-check-input custom-checkbox" type="checkbox" ' + (data.isRefined ? 'checked' : '') + ' style="pointer-events:none;">' +
                           '<label class="form-check-label text-muted" style="cursor:pointer;">' + formattedLabel + '</label>' +
                           '</div>' +
                           '<span class="text-muted small flex-shrink-0 ms-2">' + data.count + '</span>' +
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
            showMoreLimit: 1000,
            sortBy: ['isRefined', 'name:asc'],
            transformItems: function(items) {
                return items.filter(function(item) {
                    return item.label && item.label.trim() !== '';
                });
            },
            cssClasses: { 
                list: 'list-unstyled m-0', 
                item: 'm-0 p-0',
                showMore: 'text-pink text-decoration-underline small fw-semibold mt-2 mb-2 show-more-btn border-0 bg-transparent p-0',
                disabledShowMore: 'd-none'
            },
            templates: {
                item: function(data) {
                    var formattedLabel = formatDisplayName(data.label);
                    return '<div class="form-check mb-2 filter-item w-100" style="cursor:pointer;">' +
                           '<input class="form-check-input custom-checkbox" type="checkbox" ' + (data.isRefined ? 'checked' : '') + ' style="pointer-events:none;">' +
                           '<label class="form-check-label text-muted" style="cursor:pointer;">' + formattedLabel + '</label>' +
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
            showMoreLimit: 1000,
            sortBy: ['isRefined', 'name:asc'],
            transformItems: function(items) {
                return items.filter(function(item) {
                    return item.label && item.label.trim() !== '';
                });
            },
            cssClasses: { 
                list: 'list-unstyled m-0', 
                item: 'm-0 p-0',
                showMore: 'text-pink text-decoration-underline small fw-semibold mt-2 mb-2 show-more-btn border-0 bg-transparent p-0',
                disabledShowMore: 'd-none'
            },
            templates: {
                item: function(data) {
                    var formattedLabel = formatDisplayName(data.label);
                    return '<div class="form-check mb-2 filter-item w-100" style="cursor:pointer;">' +
                           '<input class="form-check-input custom-checkbox" type="checkbox" ' + (data.isRefined ? 'checked' : '') + ' style="pointer-events:none;">' +
                           '<label class="form-check-label text-muted" style="cursor:pointer;">' + formattedLabel + '</label>' +
                           '</div>';
                },
                showMoreText: function(data) {
                    return data.isShowingMore ? 'Show Less' : 'Show More';
                }
            }
        }),

        instantsearch.widgets.refinementList({
            container: '#refinement-more-options',
            attribute: 'MoreOptions',
            limit: 4,
            showMore: true,
            showMoreLimit: 1000,
            sortBy: ['isRefined', 'name:asc'],
            transformItems: function(items) {
                return items.filter(function(item) {
                    var val = (item.value || '').toLowerCase().trim();
                    var lbl = (item.label || '').toLowerCase().trim();
                    return val !== 'book now' && lbl !== 'book now';
                });
            },
            cssClasses: { 
                list: 'list-unstyled m-0', 
                item: 'm-0 p-0',
                showMore: 'text-pink text-decoration-underline small fw-semibold mt-2 mb-2 show-more-btn border-0 bg-transparent p-0',
                disabledShowMore: 'd-none'
            },
            templates: {
                item: function(data) {
                    return '<div class="form-check mb-2 filter-item w-100" style="cursor:pointer;">' +
                           '<input class="form-check-input custom-checkbox" type="checkbox" ' + (data.isRefined ? 'checked' : '') + ' style="pointer-events:none;">' +
                           '<label class="form-check-label text-muted" style="cursor:pointer;">' + data.label + '</label>' +
                           '</div>';
                },
                showMoreText: function(data) {
                    return data.isShowingMore ? 'Show Less' : 'Show More';
                }
            }
        }),

        customRangeSlider({ container: '#range-slider-price', attribute: 'Price', prefix: currencySymbol, defaultMin: 0, defaultMax: convertPrice(100000) }),
        customRangeSlider({ container: '#range-slider-duration', attribute: 'Duration', suffix: ' Days', defaultMin: 1, defaultMax: 60 }),

        instantsearch.widgets.clearRefinements({
            container: '#clear-refinements-mobile', templates: { resetLabel: 'Clear Reset' },
            cssClasses: { button: 'btn btn-clear btn-outline-secondary w-100 h-100 py-2' }
        }),

                instantsearch.widgets.pagination({
            container: '#pagination',
            padding: (function() {
                if (typeof window === 'undefined') return 3;
                var isMobileDevice = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
                var w = Math.min(window.innerWidth || 9999, (window.screen && window.screen.width) || 9999);
                return (isMobileDevice || w < 992) ? 1 : 3;
            })(),
            cssClasses: { list: 'pagination custom-pagination justify-content-center m-0', item: 'page-item', link: 'page-link', selectedItem: 'active' }
        }),

       instantsearch.widgets.hits({
            container: '#hits',
            cssClasses: { list: 'row g-4 list-unstyled m-0 p-0', item: 'col-12 col-md-6 col-lg-4' },
            templates: {
                item: function(hit) {
                    var iconsMarkup = '';
                    
                    if (hit.PriceInclusions && Array.isArray(hit.PriceInclusions) && hit.PriceInclusions.length > 0) {
                        var svgMap = {
                            "flights": {
                                label: "Flights",
                                svg: '<svg width="23" height="21" viewBox="0 0 23 21" fill="none" xmlns="http://www.w3.org/2000/svg">\r\n <path d="M22.2031 9.17188C22.5156 9.52344 22.75 9.99219 22.75 10.5C22.75 11.5547 21.8906 12.375 21.1094 12.8438C20.2891 13.3125 19.2344 13.625 18.4531 13.625H14.5078L11.1484 19.5625C10.7969 20.1484 10.1719 20.5 9.50781 20.5H7.32031C6.46094 20.5 5.875 19.7188 6.10938 18.9375L7.63281 13.625H5.5625L4.03906 15.6562C3.80469 15.9688 3.45312 16.125 3.0625 16.125H1.42188C0.757812 16.125 0.25 15.6172 0.25 14.9531C0.25 14.875 0.25 14.7578 0.289062 14.6406L1.46094 10.5L0.289062 6.39844C0.25 6.28125 0.25 6.16406 0.25 6.04688C0.25 5.42188 0.757812 4.875 1.42188 4.875H3.0625C3.45312 4.875 3.80469 5.07031 4.03906 5.38281L5.5625 7.375H7.63281L6.10938 2.10156C5.875 1.32031 6.46094 0.5 7.32031 0.5H9.50781C10.1719 0.5 10.7969 0.890625 11.1484 1.47656L14.5078 7.375H18.4531C19.2344 7.375 20.2891 7.72656 21.1094 8.23438C21.5 8.46875 21.9297 8.78125 22.2031 9.17188ZM20.4453 9.28906C19.7812 8.89844 18.9609 8.625 18.4531 8.625H14.1562C13.9219 8.625 13.7266 8.50781 13.6094 8.3125L10.0547 2.10156C9.9375 1.90625 9.74219 1.75 9.50781 1.75H7.32031L9.03906 7.84375C9.11719 8.03906 9.07812 8.23438 8.96094 8.39062C8.84375 8.54688 8.64844 8.625 8.45312 8.625H5.25C5.01562 8.625 4.85938 8.54688 4.74219 8.39062L3.0625 6.125H1.5L2.71094 10.3438C2.75 10.4609 2.75 10.5781 2.71094 10.6953L1.5 14.875H3.0625L4.74219 12.6484C4.85938 12.4922 5.01562 12.375 5.25 12.375H8.45312C8.64844 12.375 8.84375 12.4922 8.96094 12.6484C9.07812 12.8047 9.11719 13 9.03906 13.1953L7.32031 19.25H9.50781C9.74219 19.25 9.9375 19.1328 10.0547 18.9375L13.6094 12.7266C13.7266 12.5312 13.9219 12.375 14.1562 12.375H18.4531C18.9609 12.375 19.7812 12.1797 20.4453 11.75C21.1484 11.3203 21.5 10.8906 21.5 10.5C21.5 10.3438 21.4219 10.1484 21.2266 9.95312C21.0703 9.71875 20.7969 9.48438 20.4453 9.28906ZM10.6016 1.78906L10.0547 2.10156L10.6016 1.78906Z" fill="#CA568E"></path>\r\n </svg>'
                            },
                            "accommodation": {
                                label: "Accommodation",
                                svg: '<svg width="20" height="19" viewBox="0 0 20 19" fill="none" xmlns="http://www.w3.org/2000/svg">\r\n <path d="M18.6136 8.97287V3.35C18.6136 1.99276 17.4891 0.868182 16.1318 0.868182H3.72273C2.3267 0.868182 1.24091 1.99276 1.24091 3.35V8.97287C0.465341 9.51577 0 10.4077 0 11.4159V17.6205C0 17.9695 0.271449 18.2409 0.620455 18.2409C0.930682 18.2409 1.24091 17.9695 1.24091 17.6205V15.7591H18.6136V17.6205C18.6136 17.9695 18.8851 18.2409 19.2341 18.2409C19.5443 18.2409 19.8545 17.9695 19.8545 17.6205V11.4159C19.8545 10.4077 19.3504 9.51577 18.6136 8.97287ZM16.7523 8.31364H10.5477V7.07273C10.5477 6.41349 11.0906 5.83182 11.7886 5.83182H16.1318C16.7911 5.83182 17.3727 6.41349 17.3727 7.07273V8.39119C17.1401 8.35241 16.9462 8.31364 16.7523 8.31364ZM3.72273 2.10909H16.1318C16.7911 2.10909 17.3727 2.69077 17.3727 3.35V4.93991C16.9849 4.74602 16.5584 4.59091 16.1318 4.59091H11.7886C11.0131 4.59091 10.3538 4.93991 9.92727 5.48281C9.46193 4.93991 8.8027 4.59091 8.06591 4.59091H3.72273C3.25739 4.59091 2.83082 4.74602 2.48182 4.93991V3.35C2.48182 2.69077 3.02472 2.10909 3.72273 2.10909ZM2.48182 7.07273C2.48182 6.41349 3.02472 5.83182 3.72273 5.83182H8.06591C8.72514 5.83182 9.30682 6.41349 9.30682 7.07273V8.31364H3.10227C2.8696 8.31364 2.67571 8.35241 2.48182 8.39119V7.07273ZM18.6136 14.5182H1.24091V11.4159C1.24091 10.4077 2.05526 9.55455 3.10227 9.55455H16.7523C17.7605 9.55455 18.6136 10.4077 18.6136 11.4159V14.5182Z" fill="#CA568E"></path>\r\n </svg>'
                            },
                            "meals": {
                                label: "Meals",
                                svg: '<svg width="19" height="21" viewBox="0 0 19 21" fill="none" xmlns="http://www.w3.org/2000/svg">\r\n <path d="M9.57003 6.44403C9.68636 6.94815 9.68636 7.45227 9.60881 7.91761C9.49247 8.42173 9.2598 8.88707 8.94957 9.27486C8.63935 9.66264 8.21278 9.97287 7.74744 10.2055C7.2821 10.4382 6.77798 10.5545 6.23509 10.5545H5.96364V19.8614C5.96364 20.0165 5.88608 20.1716 5.76974 20.2879C5.65341 20.4043 5.4983 20.4818 5.34318 20.4818C5.14929 20.4818 4.99418 20.4043 4.87784 20.2879C4.76151 20.1716 4.72273 20.0165 4.72273 19.8614V10.5545H4.4125C3.8696 10.5545 3.36548 10.4382 2.90014 10.2055C2.4348 10.0116 2.00824 9.66264 1.69801 9.27486C1.38778 8.88707 1.15511 8.42173 1.03878 7.91761C0.922443 7.41349 0.961222 6.90937 1.07756 6.40526L1.93068 1.17017C1.96946 1.01506 2.04702 0.859943 2.16335 0.782386C2.31847 0.704829 2.47358 0.666051 2.62869 0.666051C2.78381 0.704829 2.93892 0.782386 3.01648 0.9375C3.13281 1.05384 3.17159 1.20895 3.13281 1.36406L2.27969 6.63793C2.20213 6.94815 2.20213 7.29716 2.24091 7.60739C2.31847 7.91761 2.47358 8.22784 2.66747 8.49929C2.86136 8.73196 3.13281 8.96463 3.44304 9.11974C3.75327 9.23608 4.06349 9.31364 4.4125 9.31364H6.23509C6.58409 9.31364 6.89432 9.23608 7.20455 9.08097C7.51477 8.96463 7.78622 8.73196 7.98011 8.49929C8.17401 8.22784 8.32912 7.95639 8.40668 7.64616C8.44545 7.29716 8.44545 6.98693 8.3679 6.6767L7.51477 1.36406C7.47599 1.20895 7.51477 1.05384 7.59233 0.898721C7.70866 0.782386 7.86378 0.666051 8.01889 0.666051C8.17401 0.627273 8.32912 0.666051 8.48423 0.782386C8.60057 0.859943 8.67813 1.01506 8.7169 1.17017L9.57003 6.44403ZM3.83082 8.03395C3.75327 7.99517 3.67571 7.95639 3.63693 7.87884C3.55938 7.84006 3.5206 7.7625 3.48182 7.68494C3.48182 7.60739 3.44304 7.52983 3.48182 7.45227L3.79205 1.24773C3.79205 1.17017 3.79205 1.05384 3.83082 0.976278C3.8696 0.898721 3.90838 0.859943 3.94716 0.782386C4.02472 0.743608 4.10227 0.704829 4.17983 0.666051C4.25739 0.627273 4.33494 0.627273 4.4125 0.666051C4.49006 0.666051 4.56761 0.666051 4.64517 0.704829C4.72273 0.743608 4.80028 0.782386 4.83906 0.859943C4.91662 0.898721 4.9554 0.976278 4.99418 1.05384C4.99418 1.13139 5.03295 1.20895 5.03295 1.28651L4.72273 7.49105C4.68395 7.64616 4.64517 7.80128 4.52884 7.91761C4.4125 8.03395 4.25739 8.07273 4.10227 8.07273H4.06349C3.98594 8.07273 3.90838 8.07273 3.83082 8.03395ZM6.11875 7.91761C6.00241 7.80128 5.96364 7.64616 5.96364 7.49105L5.65341 1.28651C5.61463 1.13139 5.69219 0.976278 5.80852 0.859943C5.88608 0.743608 6.04119 0.666051 6.23509 0.666051C6.31264 0.627273 6.3902 0.666051 6.46776 0.666051C6.54531 0.704829 6.62287 0.743608 6.66165 0.782386C6.7392 0.859943 6.77798 0.9375 6.81676 1.01506C6.85554 1.05384 6.85554 1.17017 6.89432 1.24773L7.20455 7.45227C7.20455 7.52983 7.16577 7.60739 7.16577 7.68494C7.12699 7.7625 7.08821 7.84006 7.01065 7.87884C6.97188 7.95639 6.89432 7.99517 6.81676 8.03395C6.7392 8.07273 6.66165 8.07273 6.58409 8.07273C6.3902 8.07273 6.23509 8.03395 6.11875 7.91761ZM18.3727 3.69077V19.8226C18.3727 20.0165 18.2952 20.1716 18.1788 20.2879C18.0625 20.4043 17.9074 20.443 17.7523 20.443C17.5584 20.443 17.4033 20.4043 17.2869 20.2879C17.1706 20.1716 17.1318 20.0165 17.1318 19.8226V14.2385H14.6112C14.2622 14.2385 13.952 14.1997 13.6418 14.0834C13.3315 13.9283 13.0601 13.7732 12.8274 13.5405C12.5947 13.3078 12.4396 12.9976 12.2845 12.7261C12.1682 12.4159 12.1294 12.0669 12.1294 11.7567L12.1682 7.33594C12.1682 5.90114 12.5947 4.50511 13.3703 3.34176C14.1847 2.13963 15.3092 1.24773 16.6665 0.704829C16.8216 0.627273 17.0543 0.588494 17.2482 0.627273C17.442 0.627273 17.6359 0.704829 17.7911 0.821165C17.9849 0.9375 18.1013 1.09261 18.2176 1.28651C18.2952 1.44162 18.3727 1.63551 18.3727 1.86818V3.69077ZM17.1318 1.86818C16.0072 2.29474 15.0766 3.03153 14.4173 4.03977C13.7581 5.00923 13.3703 6.17259 13.4091 7.33594L13.3703 11.7567C13.3703 11.9506 13.4091 12.1057 13.4479 12.2608C13.5254 12.4159 13.603 12.5322 13.7193 12.6486C13.8357 12.7649 13.9908 12.8425 14.1071 12.92C14.2622 12.9976 14.4561 13.0364 14.6112 13.0364H17.0543L17.1318 1.86818Z" fill="#CA568E"></path>\r\n </svg>'
                            },
                            "transfer": {
                                label: "Transfer",
                                svg: '<svg width="18" height="21" viewBox="0 0 18 21" fill="none" xmlns="http://www.w3.org/2000/svg">\r\n <path d="M2.79205 14.2773C2.79205 13.7732 3.17983 13.3466 3.72273 13.3466C4.22685 13.3466 4.65341 13.7732 4.65341 14.2773C4.65341 14.8202 4.22685 15.208 3.72273 15.208C3.17983 15.208 2.79205 14.8202 2.79205 14.2773ZM14.5807 14.2773C14.5807 14.8202 14.1541 15.208 13.65 15.208C13.1071 15.208 12.7193 14.8202 12.7193 14.2773C12.7193 13.7732 13.1071 13.3466 13.65 13.3466C14.1541 13.3466 14.5807 13.7732 14.5807 14.2773ZM17.0625 3.38054C17.2952 3.69077 17.3727 4.03977 17.3727 4.35V15.5182C17.3727 16.4489 16.8686 17.2632 16.1318 17.6898V19.8614C16.1318 20.2104 15.8216 20.4818 15.5114 20.4818C15.1624 20.4818 14.8909 20.2104 14.8909 19.8614V18H2.48182V19.8614C2.48182 20.2104 2.17159 20.4818 1.86136 20.4818C1.51236 20.4818 1.24091 20.2104 1.24091 19.8614V17.6898C0.465341 17.2632 0 16.4489 0 15.5182V4.35C0 4.03977 0.0387784 3.65199 0.310227 3.34176C1.24091 2.21719 3.87784 0.627273 8.68636 0.627273C13.8051 0.627273 16.1706 2.17841 17.0625 3.38054ZM1.24091 4.35H16.1318C16.1318 4.19489 16.093 4.15611 16.0543 4.11733C15.4726 3.38054 13.5724 1.86818 8.68636 1.86818C4.11051 1.86818 1.90014 3.38054 1.27969 4.15611C1.24091 4.15611 1.24091 4.19489 1.24091 4.35ZM1.24091 5.59091V10.5545H16.1318V5.59091H1.24091ZM16.1318 11.7955H1.24091V15.5182C1.24091 16.2162 1.78381 16.7591 2.48182 16.7591H14.8909C15.5501 16.7591 16.1318 16.2162 16.1318 15.5182V11.7955Z" fill="#CA568E"></path>\r\n </svg>'
                            }
                        };
                        hit.PriceInclusions.forEach(function(inclusion) {
                            var key = inclusion.toLowerCase().trim();
                            var item = svgMap[key];
                            if (item) {
                                if (iconsMarkup !== '') { iconsMarkup += '<span class="pink-v-line icon-divider"></span>'; }
                                iconsMarkup += '<div class="icon-wrapper">' + item.svg + '<span class="tooltip-text">' + item.label + '</span></div>';
                            }
                        });
                    }

                    var title = hit.TourName || hit.Title || '';
                    var desc = hit.Description || '';
                    var duration = hit.Duration ? hit.Duration.toString().replace('Days', '').trim() : '';
                    var badgeText = hit.Destinations && hit.Destinations.length > 0 ? formatDisplayName(hit.Destinations[0]).toUpperCase() : '';

                    var rawPrice = hit.Price || 0;
                    var rawOrig = hit.OriginalPrice || 0;
                    var currentPriceNum = convertPrice(rawPrice);
                    var originalPriceNum = convertPrice(rawOrig);
                    var hasDiscount = appliesDiscount && (hit.HasDiscount || (rawOrig > 0 && rawPrice < rawOrig));

                    // 1. Duration / Original Price line
                    var durationText = duration ? (duration + ' DAYS') : '';
                    var originalPriceMarkup = '';

                    if (currentPriceNum > 0) {
                        var fromLabel = durationText ? (durationText + ' FROM ') : 'FROM ';
                        if (hasDiscount && originalPriceNum > currentPriceNum) {
                            originalPriceMarkup = '<div class="original-price text-uppercase">' + fromLabel + '<del>' + originalPriceNum.toLocaleString() + '</del></div>';
                        } else {
                            originalPriceMarkup = '<div class="original-price text-uppercase">' + fromLabel + '</div>';
                        }
                    } else if (durationText) {
                        originalPriceMarkup = '<div class="original-price text-uppercase">' + durationText + '</div>';
                    }

                    // 2. Current Price line
                    var currentPriceMarkup = '';
                    if (currentPriceNum > 0) {
                        currentPriceMarkup = '<div class="current-price text-pink">' + currencySymbol + currentPriceNum.toLocaleString() + ' <span style="font-size: 0.55em; margin-left: 2px; font-weight: 700;">' + currencyCode + '</span></div>';
                    }

                    // 3. Price Note line (ONLY if price is present)
                    var priceNoteMarkup = '';
                    if (currentPriceNum > 0) {
                        priceNoteMarkup = '<div class="price-note mb-3">Per person <span class="pink-v-line"></span> twin share</div>';
                    }

                    return '<a href="' + (hit.Url || '#') + '" class="product-card-vertical h-100 text-decoration-none">' +
                             '<div class="img-wrap">' +
                               '<img loading="lazy" src="' + (hit.ImageUrl || '') + '" alt="' + title + '">' +
                               (badgeText ? '<span class="card-badge">' + badgeText + '</span>' : '') +
                             '</div>' +
                             '<div class="card-body-content flex-grow-1 d-flex flex-column">' +
                               '<h3 class="card-title text-dark">' + title + '</h3>' +
                               '<p class="card-desc three-line-desc text-muted">' + desc + '</p>' +
                               '<div class="tahiti-v2-price-block mt-auto">' +
                                 originalPriceMarkup +
                                 currentPriceMarkup +
                                 priceNoteMarkup +
                                 (iconsMarkup ? '<div class="card-icons text-pink">' + iconsMarkup + '</div>' : '') +
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
        
        // Dynamically remove Show More button if facet data has 4 or fewer items (e.g. Alaska with 3-4 items)
        var facetWidgets = [
            { sel: '#refinement-travel-style', attr: 'TravelStyles' },
            { sel: '#refinement-start-location', attr: 'StartLocation' },
            { sel: '#refinement-end-location', attr: 'EndLocation' },
            { sel: '#refinement-more-options', attr: 'MoreOptions' }
        ];

        facetWidgets.forEach(function(fw) {
            var el = document.querySelector(fw.sel);
            if (!el) return;
            var btn = el.querySelector('.show-more-btn, button[class*="showMore"]');
            if (!btn) return;

            var facetCount = 0;
            if (results && results._rawResults && results._rawResults[0] && results._rawResults[0].facets && results._rawResults[0].facets[fw.attr]) {
                facetCount = Object.keys(results._rawResults[0].facets[fw.attr]).length;
            } else if (results && results.facets && results.facets[fw.attr]) {
                facetCount = Object.keys(results.facets[fw.attr]).length;
            } else if (results && typeof results.getFacetValues === 'function') {
                var fData = results.getFacetValues(fw.attr);
                if (Array.isArray(fData)) {
                    facetCount = fData.length;
                } else if (fData && fData.data) {
                    facetCount = Array.isArray(fData.data) ? fData.data.length : Object.keys(fData.data).length;
                }
            }

            var isShowingMore = btn.textContent && btn.textContent.toLowerCase().indexOf('less') !== -1;
            var isDisabled = btn.disabled || btn.hasAttribute('disabled') || btn.classList.contains('ais-RefinementList-showMore--disabled');

            // If 4 or fewer options exist (or 0 options, or disabled): always remove the Show More button
            if ((facetCount > 0 && facetCount <= 4) || isDisabled) {
                btn.classList.add('d-none');
                btn.style.setProperty('display', 'none', 'important');
            } else if (facetCount > 4 || isShowingMore) {
                btn.classList.remove('d-none');
                btn.style.removeProperty('display');
            }
        });

        // Interdependent dynamic dropdowns: Destination and Experience update each other
        var uiState = (search.getUiState() && search.getUiState()[searchIndex]) || {};
        var activeDest = (uiState.menu && uiState.menu.Destinations) || null;
        var activeExp = (uiState.menu && uiState.menu.Experiences) || null;

        // 1. Update Experience dropdown based on activeDest
        fetchExperiencesForDestination(activeDest, function(expList) {
            if (userHasInteracted && activeDest && activeExp && !listContains(expList, activeExp)) {
                // Incompatible experience selected for this destination -> clear it to prevent 0 results!
                if (search.renderState && search.renderState[searchIndex] && search.renderState[searchIndex].menu && search.renderState[searchIndex].menu.Experiences) {
                    search.renderState[searchIndex].menu.Experiences.refine('');
                }
                activeExp = null;
            }
            updateTomSelect('#searchbox-experience', expList, activeExp, 'Select experience');
        });

        // 2. Update Destination dropdown based on activeExp
        fetchDestinationsForExperience(activeExp, function(destList) {
            if (userHasInteracted && activeExp && activeDest && !listContains(destList, activeDest)) {
                // Incompatible destination selected for this experience -> clear it to prevent 0 results!
                if (search.renderState && search.renderState[searchIndex] && search.renderState[searchIndex].menu && search.renderState[searchIndex].menu.Destinations) {
                    search.renderState[searchIndex].menu.Destinations.refine('');
                }
                activeDest = null;
            }
            updateTomSelect('#searchbox-destination', destList, activeDest, 'Select destination');
        });

            function trimPaginationOnMobile() {
        var container = document.getElementById('pagination');
        if (!container) return;
        var list = container.querySelector('.pagination');
        if (!list) return;

        var isMobile = (window.innerWidth < 992) || /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
        var items = Array.from(list.querySelectorAll('.page-item'));

        if (!isMobile) {
            items.forEach(function(el) { el.style.display = ''; });
            return;
        }

        var numItems = items.filter(function(el) {
            return /^\d+$/.test(el.textContent.trim());
        });

        if (numItems.length <= 3) return;

        var activeNumIdx = numItems.findIndex(function(el) { return el.classList.contains('active'); });
        if (activeNumIdx === -1) activeNumIdx = 0;

        var startIdx = Math.max(0, Math.min(activeNumIdx - 1, numItems.length - 3));
        var endIdx = startIdx + 2;

        numItems.forEach(function(el, i) {
            if (i >= startIdx && i <= endIdx) {
                el.style.display = '';
            } else {
                el.style.display = 'none';
            }
        });
    }

    window.addEventListener('resize', trimPaginationOnMobile);

        var nbHits = results ? results.nbHits : 0;
        var hitsPerPage = results ? results.hitsPerPage : 30;
        var pagContainer = document.getElementById('pagination');
        var navContainer = document.getElementById('pagination-nav');

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
                trimPaginationOnMobile();
            }
        }
    });

    search.start();

    // Fetch master facet values for Destinations and Experiences so dropdown options are always populated
    masterIndex.search('', { facets: ['Destinations', 'Experiences'], hitsPerPage: 0 })
        .then(function(res) {
            const destFacets = (res.facets && res.facets['Destinations']) || {};
            const expFacets = (res.facets && res.facets['Experiences']) || {};

            masterDestinations = Object.keys(destFacets).sort();
            masterExperiences = Object.keys(expFacets).sort();

            const uiState = (search.getUiState() && search.getUiState()[searchIndex]) || {};
            const activeDest = (uiState.menu && uiState.menu.Destinations) || null;
            const activeExp = (uiState.menu && uiState.menu.Experiences) || null;

            fetchExperiencesForDestination(activeDest, function(expList) {
                updateTomSelect('#searchbox-experience', expList, activeExp, 'Select experience');
            });

            fetchDestinationsForExperience(activeExp, function(destList) {
                updateTomSelect('#searchbox-destination', destList, activeDest, 'Select destination');
            });
        })
        .catch(function(err) {
            console.warn('Master facet preload error:', err);
        });

    // FIND button form submit handler on search page
    const searchForm = document.querySelector('.search-bar-wrap form');
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            e.preventDefault();
            userHasInteracted = true;
            const kwEl = document.querySelector('#searchbox-keyword');
            const destEl = document.querySelector('#searchbox-destination');
            const expEl = document.querySelector('#searchbox-experience');

            const rawDestVal = destEl && destEl.tomselect ? destEl.tomselect.getValue() : (destEl ? destEl.value : '');
            const rawExpVal = expEl && expEl.tomselect ? expEl.tomselect.getValue() : (expEl ? expEl.value : '');
            const destVal = formatDisplayName(rawDestVal);
            const expVal = formatDisplayName(rawExpVal);
            const kwVal = kwEl ? kwEl.value.trim() : '';

            if (search.renderState && search.renderState[searchIndex]) {
                if (search.renderState[searchIndex].searchBox) {
                    search.renderState[searchIndex].searchBox.refine(kwVal);
                }
                if (search.renderState[searchIndex].menu) {
                    if (search.renderState[searchIndex].menu.Destinations) {
                        if (destVal === 'all' || !destVal) {
                            const cur = search.helper && search.helper.getHierarchicalFacetBreadcrumb('Destinations')[0];
                            if (cur) search.renderState[searchIndex].menu.Destinations.refine(cur);
                            else search.renderState[searchIndex].menu.Destinations.refine('');
                        } else {
                            search.renderState[searchIndex].menu.Destinations.refine(destVal);
                        }
                    }
                    if (search.renderState[searchIndex].menu.Experiences) {
                        if (expVal === 'all' || !expVal) {
                            const cur = search.helper && search.helper.getHierarchicalFacetBreadcrumb('Experiences')[0];
                            if (cur) search.renderState[searchIndex].menu.Experiences.refine(cur);
                            else search.renderState[searchIndex].menu.Experiences.refine('');
                        } else {
                            search.renderState[searchIndex].menu.Experiences.refine(expVal);
                        }
                    }
                }
            }
        });
    }
})();

// Auto-close filter slider on Clear Reset or Apply Filters
function closeFilterSlider() {
    var el = document.getElementById('filterOffcanvas');
    if (!el) return;
    try {
        if (typeof bootstrap !== 'undefined' && bootstrap.Offcanvas) {
            var inst = bootstrap.Offcanvas.getInstance(el) || new bootstrap.Offcanvas(el);
            if (inst) inst.hide();
        }
    } catch (err) { }
    el.classList.remove('show');
    document.querySelectorAll('.offcanvas-backdrop').forEach(function(b) { b.remove(); });
    document.body.classList.remove('modal-open');
    document.body.style.removeProperty('overflow');
    document.body.style.removeProperty('padding-right');
}

document.addEventListener('click', function(e) {
    if (!e.target) return;
    if (e.target.closest('#clear-refinements-mobile')) {
        setTimeout(closeFilterSlider, 150);
    }
    if (e.target.closest('#apply-filters-btn') || (e.target.closest('#filterOffcanvas') && e.target.classList && e.target.classList.contains('btn-pink'))) {
        closeFilterSlider();
    }
});
