(function () {
    const container = document.getElementById('algolia-landing');

    // Safety check in case the script runs on a different page
    if (!container) return;

    const APP_ID = container.dataset.algoliaAppId;
    const API_KEY = container.dataset.algoliaSearchKey;
    const INDEX = container.dataset.algoliaIndex;

    if (!APP_ID || !API_KEY || !INDEX) return;

    // Parse featured article URLs to exclude from Algolia grid
    const featuredUrlsRaw = container.dataset.featuredUrls || '';
    const FEATURED_SLUGS = featuredUrlsRaw
        .split(',')
        .map(u => u.trim().replace(/^\/+|\/+$/g, '').split('/').filter(Boolean).pop() || '')
        .filter(Boolean)
        .map(s => s.toLowerCase());

    // Parse excluded article object IDs to filter out at query time
    const excludedIdsRaw = container.dataset.excludedIds || '';
    const EXCLUDED_IDS = excludedIdsRaw
        .split(',')
        .map(id => id.trim().toLowerCase())
        .filter(Boolean);

    const EXCLUDED_FILTER = EXCLUDED_IDS.length > 0
        ? EXCLUDED_IDS.map(id => 'NOT objectID:' + id).join(' AND ')
        : '';

    // Dynamic formatting helper: converts PascalCase / unspaced names to formatted display names with spaces
    function formatDisplayName(str) {
        if (!str || str === 'all') return str;
        var clean = str.replace(/-[a-z0-9]{8}$/i, '');
        clean = clean.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
        clean = clean.replace(/\s*&\s*/g, ' & ');
        return clean.replace(/\s+/g, ' ').trim();
    }

    // Value matching helper for case and space insensitivity
    function matchesVal(a, b) {
        if (!a || !b) return false;
        var sA = formatDisplayName(a).toLowerCase();
        var sB = formatDisplayName(b).toLowerCase();
        var uA = a.replace(/\s+/g, '').toLowerCase();
        var uB = b.replace(/\s+/g, '').toLowerCase();
        return sA === sB || uA === uB;
    }

    function expandFilterItem(str) {
        if (typeof str !== 'string') return [str];
        var colonIdx = str.indexOf(':');
        if (colonIdx === -1) return [str];
        var attr = str.slice(0, colonIdx);
        var val = str.slice(colonIdx + 1);

        var variants = [val];
        var unspaced = val.replace(/\s+/g, '');
        if (unspaced && variants.indexOf(unspaced) === -1) variants.push(unspaced);
        var formatted = formatDisplayName(val);
        if (formatted && variants.indexOf(formatted) === -1) variants.push(formatted);

        return variants.map(function(v) { return attr + ':' + v; });
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
            if (EXCLUDED_FILTER) {
                if (copy.filters && copy.filters.trim()) {
                    if (copy.filters.indexOf(EXCLUDED_FILTER) === -1) {
                        copy.filters = '(' + copy.filters + ') AND (' + EXCLUDED_FILTER + ')';
                    }
                } else {
                    copy.filters = EXCLUDED_FILTER;
                }
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
                } catch (e) { }
            }
            if (EXCLUDED_FILTER) {
                var curFilters = urlParams.get('filters');
                if (curFilters && curFilters.trim()) {
                    if (curFilters.indexOf(EXCLUDED_FILTER) === -1) {
                        urlParams.set('filters', '(' + curFilters + ') AND (' + EXCLUDED_FILTER + ')');
                    }
                } else {
                    urlParams.set('filters', EXCLUDED_FILTER);
                }
            }
            return urlParams.toString();
        }
        return params;
    }

    const rawSearchClient = algoliasearch(APP_ID, API_KEY);
    const searchClient = {
        search: function(requests) {
            var transformed = (requests || []).map(function(req) {
                var copy = Object.assign({}, req);
                if (copy.params) {
                    copy.params = transformParams(copy.params);
                } else if (EXCLUDED_FILTER) {
                    copy.params = { filters: EXCLUDED_FILTER };
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
                    } else if (EXCLUDED_FILTER) {
                        copy.params = { filters: EXCLUDED_FILTER };
                    }
                    return copy;
                });
                return rawSearchClient.searchForFacetValues(transformed);
            }
            return Promise.resolve([]);
        }
    };

    const search = instantsearch({
        indexName: INDEX,
        searchClient,
        routing: {
            router: instantsearch.routers.history({
                cleanUrlOnDispose: true
            }),
            stateMapping: {
                stateToRoute: function(uiState) {
                    const indexUiState = uiState[INDEX] || {};
                    const route = {};
                    const indexRoute = {};

                    if (indexUiState.query) {
                        indexRoute.query = indexUiState.query;
                    }
                    if (indexUiState.hitsPerPage && indexUiState.hitsPerPage !== 30) {
                        indexRoute.hitsPerPage = indexUiState.hitsPerPage;
                    }
                    if (indexUiState.page && indexUiState.page > 1) {
                        indexRoute.page = indexUiState.page;
                    }
                    if (indexUiState.menu) {
                        const menu = {};
                        if (indexUiState.menu.destination) {
                            menu.destinations = formatDisplayName(indexUiState.menu.destination);
                        }
                        if (indexUiState.menu.category) {
                            menu.categories = formatDisplayName(indexUiState.menu.category);
                        }
                        if (indexUiState.menu.experience) {
                            menu.experiences = formatDisplayName(indexUiState.menu.experience);
                        }
                        if (Object.keys(menu).length > 0) {
                            indexRoute.menu = menu;
                        }
                    }

                    if (Object.keys(indexRoute).length > 0) {
                        route[INDEX] = indexRoute;
                    }
                    return route;
                },
                routeToState: function(routeState) {
                    if (!routeState) return {};
                    const indexRoute = routeState[INDEX] || {};
                    const indexUiState = {};

                    const q = indexRoute.query || routeState.query;
                    if (q) indexUiState.query = q;

                    const hpp = indexRoute.hitsPerPage || routeState.hitsPerPage;
                    if (hpp) indexUiState.hitsPerPage = parseInt(hpp, 10);

                    const page = indexRoute.page || routeState.page;
                    if (page) indexUiState.page = parseInt(page, 10);

                    const menu = indexRoute.menu || routeState.menu || {};
                    const dest = menu.destinations || menu.destination || routeState.destination || routeState.destinations;
                    const cat = menu.categories || menu.category || routeState.category || routeState.categories;
                    const exp = menu.experiences || menu.experience || routeState.experience || routeState.experiences;

                    if (dest || cat || exp) {
                        indexUiState.menu = {};
                        if (dest) indexUiState.menu.destination = formatDisplayName(dest);
                        if (cat) indexUiState.menu.category = formatDisplayName(cat);
                        if (exp) indexUiState.menu.experience = formatDisplayName(exp);
                    }

                    return { [INDEX]: indexUiState };
                }
            }
        }
    });

    // Custom Search Box
    const customSearchBox = instantsearch.connectors.connectSearchBox(
        function (renderOptions, isFirstRender) {
            const { query, refine, widgetParams } = renderOptions;
            const input = document.querySelector(widgetParams.container);
            if (isFirstRender) {
                input.addEventListener('input', e => refine(e.target.value));
            }
            input.value = query;
        }
    );

    // Custom Dropdown (Menu Facet with TomSelect)
    const customDropdown = instantsearch.connectors.connectMenu(
        function (renderOptions, isFirstRender) {
            const { items, refine, widgetParams } = renderOptions;
            const selectEl = document.querySelector(widgetParams.container);
            if (!selectEl) return;

            let isSyncing = false;

            // Initialize TomSelect immediately without arbitrary delay
            if (!selectEl.tomselect && typeof window.TomSelect !== 'undefined') {
                new window.TomSelect(selectEl, {
                    controlInput: null,
                    create: false,
                    maxOptions: 1000,
                    wrapperClass: 'ts-wrapper blogs-filter-select-wrapper'
                });

                selectEl.tomselect.on('change', function (val) {
                    if (isSyncing) return;
                    refine(val === 'all' ? '' : val);
                });
            }

            const helperResults = (search && search.helper && search.helper.lastResults) ? search.helper.lastResults : null;
            const currentHits = (renderOptions.results && typeof renderOptions.results.nbHits === 'number')
                ? renderOptions.results.nbHits
                : (helperResults && typeof helperResults.nbHits === 'number' ? helperResults.nbHits : null);

            // Determine active value
            let activeValue = null;
            const refinedItem = items.find(function (i) { return i.isRefined; });
            if (refinedItem) {
                activeValue = formatDisplayName(refinedItem.value);
            } else {
                const uiState = search.getUiState();
                const indexState = (uiState && uiState[INDEX]) || {};
                if (indexState.menu && indexState.menu[widgetParams.attribute]) {
                    activeValue = formatDisplayName(indexState.menu[widgetParams.attribute]);
                }
            }

            if (selectEl.tomselect) {
                const ts = selectEl.tomselect;

                // Match active item among options
                let matchedActiveVal = 'all';
                let activeText = 'See all';

                const newOptions = {};
                newOptions['all'] = { value: 'all', text: 'See all' };

                items.forEach(function (item) {
                    const formatted = formatDisplayName(item.value);
                    const label = formatDisplayName(item.label);
                    const count = (item.isRefined && typeof currentHits === 'number') ? currentHits : item.count;
                    const text = label + ' (' + count + ')';

                    newOptions[formatted] = { value: formatted, text: text };

                    if (activeValue && (matchesVal(formatted, activeValue) || matchesVal(item.value, activeValue))) {
                        matchedActiveVal = formatted;
                        activeText = text;
                    }
                });

                const curKeys = Object.keys(ts.options).filter(function(k) { return k !== 'all'; }).sort().join('|');
                const newKeys = Object.keys(newOptions).filter(function(k) { return k !== 'all'; }).sort().join('|');

                if (curKeys !== newKeys) {
                    isSyncing = true;
                    ts.clear(true);
                    ts.clearOptions();
                    Object.keys(newOptions).forEach(function (k) {
                        ts.addOption(newOptions[k]);
                    });
                    ts.refreshOptions(false);
                    isSyncing = false;
                }

                if (ts.getValue() !== matchedActiveVal) {
                    isSyncing = true;
                    ts.setValue(matchedActiveVal, true);
                    isSyncing = false;
                }

                if (typeof ts.getItem === 'function') {
                    const itemEl = ts.getItem(matchedActiveVal);
                    if (itemEl && activeText) {
                        itemEl.textContent = activeText;
                    }
                }
            } else {
                selectEl.innerHTML = '<option value="all">See all</option>' +
                    items.map(function (item) {
                        const formatted = formatDisplayName(item.value);
                        const label = formatDisplayName(item.label);
                        const count = (item.isRefined && typeof currentHits === 'number') ? currentHits : item.count;
                        const isSelected = activeValue && (matchesVal(formatted, activeValue) || matchesVal(item.value, activeValue));
                        return '<option value="' + formatted + '" ' + (isSelected ? 'selected' : '') + '>' + label + ' (' + count + ')</option>';
                    }).join('');
            }
        }
    );

    // Custom Hits Per Page Connector
    const customHitsPerPage = instantsearch.connectors.connectHitsPerPage(
        function (renderOptions, isFirstRender) {
            const { items, refine } = renderOptions;
            const viewSelect = document.getElementById('viewFilter');

            if (isFirstRender && viewSelect) {
                viewSelect.addEventListener('change', function () {
                    refine(parseInt(this.value, 10));
                });
            }

            // Keep the select in sync with the current hitsPerPage
            if (viewSelect) {
                const currentItem = items.find(i => i.isRefined);
                if (currentItem) {
                    viewSelect.value = currentItem.value.toString();
                }
            }
        }
    );

    // Add all widgets
        function trimPaginationOnMobile() {
        var container = document.getElementById('algolia-pagination');
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

    const widgets = [
        customHitsPerPage({
            items: [
                { label: '30', value: 30, default: true },
                { label: '60', value: 60 }
            ]
        }),

        customSearchBox({ container: '#searchInput' }),
        customDropdown({ container: '#destFilter', attribute: 'destination', limit: 50 }),
        customDropdown({ container: '#catFilter', attribute: 'category', limit: 50 }),
        customDropdown({ container: '#expFilter', attribute: 'experience', limit: 50 }),

        instantsearch.widgets.hits({
            container: '#algolia-hits',
            cssClasses: { list: 'row g-4 list-unstyled m-0 justify-content-center', item: 'col-12 col-md-6 col-lg-4' },
            templates: {
                item(hit) {
                    let articleUrl = '#';
                    if (hit.url) {
                        const cleanUrl = hit.url.replace(/\/+$/, '');
                        const segments = cleanUrl.split('/').filter(Boolean);
                        const slug = segments.length > 0 ? segments[segments.length - 1] : '';
                        articleUrl = slug ? ('/' + slug) : (hit.url.startsWith('/') ? hit.url : '/' + hit.url);
                    }

                    // Format date: "MMM dd, yyyy" (e.g. "Aug 04, 2026")
                    let formattedDate = '';
                    if (hit.date) {
                        const d = new Date(hit.date);
                        if (!isNaN(d.getTime()) && d.getFullYear() > 1900) {
                            const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
                            const month = months[d.getMonth()];
                            const day = String(d.getDate()).padStart(2, '0');
                            const year = d.getFullYear();
                            formattedDate = month + ' ' + day + ', ' + year;
                        }
                    }

                    // Extract Category string (e.g. "Culture")
                    let category = '';
                    if (hit.category) {
                        if (Array.isArray(hit.category)) {
                            category = hit.category.filter(Boolean).map(formatDisplayName).join(', ');
                        } else if (typeof hit.category === 'string') {
                            category = formatDisplayName(hit.category);
                        }
                    }

                    // Extract Destination string (e.g. "Fiji")
                    let destination = '';
                    if (hit.destination) {
                        if (Array.isArray(hit.destination)) {
                            destination = hit.destination.filter(Boolean).map(formatDisplayName).join(', ');
                        } else if (typeof hit.destination === 'string') {
                            destination = formatDisplayName(hit.destination);
                        }
                    }

                    // Prioritize Category, then Destination, then just 'Holidays'
                    let relatedText = '';
                    if (category && category.trim()) {
                        relatedText = category.trim() + ' ';
                    } else if (destination && destination.trim()) {
                        relatedText = destination.trim() + ' ';
                    }

                    // Card top tag: if category is present then category + date, if category is empty then destination + date
                    var displayTag = (category && category.trim()) ? category.trim() : ((destination && destination.trim()) ? destination.trim() : '');

                    return '<div class="blog-list-card d-flex flex-column h-100">' +
                            (hit.image ? 
                                '<a href="' + articleUrl + '" class="d-block text-decoration-none">' +
                                    '<img src="' + hit.image + '" class="blog-list-img" alt="' + (hit.title || '') + '" loading="lazy" />' +
                                '</a>' :
                                '<div class="blog-list-img bg-light d-flex align-items-center justify-content-center" style="min-height: 180px;">' +
                                    '<span class="text-muted small text-center p-2">' + (hit.title || '') + '</span>' +
                                '</div>'
                            ) +
                            '<div class="blog-list-body flex-grow-1 d-flex flex-column p-4">' +
                                '<div class="d-flex justify-content-between align-items-center mb-2">' +
                                    (displayTag ? '<span class="blog-list-tag m-0 fw-bold" style="font-size:10px; text-transform:uppercase;">' + displayTag + '</span>' : '') +
                                    (formattedDate ? '<span class="text-muted ms-auto" style="font-size:11px;">' + formattedDate + '</span>' : '') +
                                '</div>' +
                                '<a href="' + articleUrl + '" class="text-decoration-none text-dark d-flex flex-column flex-grow-1">' +
                                    '<h3 class="blog-list-title mb-2 fs-5 fw-bold">' + (hit.title || '') + '</h3>' +
                                    (hit.summary ? '<p class="text-muted small mb-3 flex-grow-1" style="display:-webkit-box; -webkit-line-clamp:2; -webkit-box-orient:vertical; overflow:hidden;">' + hit.summary + '</p>' : '') +
                                '</a>' +
                                '<span class="text-pink fw-bold mt-auto" style="font-size:12px; letter-spacing:0.5px;">' +
                                    'Related: ' + relatedText + 'Holidays' +
                                '</span>' +
                            '</div>' +
                        '</div>';
                },
                empty: '<div class="col-12 text-center py-5 text-muted" style="font-size: 1.1rem;">No results found.</div>'
            }
        }),

                instantsearch.widgets.pagination({
            container: '#algolia-pagination',
            padding: (function() {
                if (typeof window === 'undefined') return 3;
                var isMobileDevice = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
                var w = Math.min(window.innerWidth || 9999, (window.screen && window.screen.width) || 9999);
                return (isMobileDevice || w < 992) ? 1 : 3;
            })(),
            cssClasses: {
                list: 'pagination custom-pagination justify-content-center m-0',
                item: 'page-item',
                link: 'page-link',
                selectedItem: 'active'
            }
        }),

        // Stats & Visibility Widget
        {
            init() { },
            render(opts) {
                if (!opts.results) return;

                const nbHits = opts.results.nbHits;
                const hitsPerPage = opts.results.hitsPerPage;

                // Display count matches total Algolia dashboard records (including top featured articles)
                let displayCount = nbHits;
                if (EXCLUDED_IDS.length > 0) {
                    const uiState = (search && search.getUiState && search.getUiState()) || {};
                    const indexState = uiState[INDEX] || {};
                    const q = (indexState.query || '').trim().toLowerCase();
                    const menu = indexState.menu || {};
                    const dest = (menu.destination || '').trim().toLowerCase();
                    const cat = (menu.category || '').trim().toLowerCase();
                    const exp = (menu.experience || '').trim().toLowerCase();

                    const isFiltered = Boolean(q || dest || cat || exp);

                    if (!isFiltered) {
                        displayCount = nbHits + EXCLUDED_IDS.length;
                    } else {
                        // Check if active filters match any of the featured articles
                        let matchingFeatured = 0;
                        const featCards = document.querySelectorAll('#featured-section .blog-list-card');
                        featCards.forEach(function (card) {
                            let match = true;
                            const title = (card.querySelector('.blog-list-title') ? card.querySelector('.blog-list-title').textContent : '').toLowerCase();
                            const tag = (card.querySelector('.blog-list-tag') ? card.querySelector('.blog-list-tag').textContent : '').toLowerCase();
                            const link = (card.querySelector('a') ? card.querySelector('a').getAttribute('href') : '').toLowerCase();

                            if (q && !title.includes(q) && !tag.includes(q)) match = false;
                            if (dest && !link.includes(dest.replace(/\s+/g, '-')) && !tag.includes(dest)) match = false;
                            if (cat && !tag.includes(cat)) match = false;

                            if (match) matchingFeatured++;
                        });
                        displayCount = nbHits + matchingFeatured;
                    }
                }

                // Always update the results count from Algolia
                const countEl = document.getElementById('algolia-stats-count');
                if (countEl) countEl.textContent = displayCount;

                // Show/hide pagination nav (tour landing style)
                const pagContainer = document.getElementById('algolia-pagination');
                const navContainer = document.getElementById('pagination-nav');

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
            }
        }
    ];

    if (EXCLUDED_FILTER) {
        widgets.unshift(instantsearch.widgets.configure({
            filters: EXCLUDED_FILTER
        }));
    }

    search.addWidgets(widgets);

    search.start();
})();
