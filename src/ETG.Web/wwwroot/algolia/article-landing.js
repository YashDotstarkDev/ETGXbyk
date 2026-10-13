(function () {
    const container = document.getElementById('algolia-landing');

    // Safety check in case the script runs on a different page
    if (!container) return;

    const APP_ID = container.dataset.algoliaAppId;
    const API_KEY = container.dataset.algoliaSearchKey;
    const INDEX = container.dataset.algoliaIndex;
    const TOTAL_ARTICLES = container.dataset.totalArticles; // Grabbed from HTML data attribute
    const PAGE_SIZE = parseInt(container.dataset.pageSize, 10) || 30;

    if (!APP_ID || !API_KEY || !INDEX) return;

    const searchClient = algoliasearch(APP_ID, API_KEY);
    const search = instantsearch({ indexName: INDEX, searchClient, routing: false });
    let isAlgoliaActive = false;

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

    const customDropdown = instantsearch.connectors.connectMenu(
        function (renderOptions, isFirstRender) {
            const { items, refine, widgetParams } = renderOptions;
            const selectEl = document.querySelector(widgetParams.container);

            if (isFirstRender) {
                setTimeout(() => {
                    if (!selectEl.tomselect && typeof window.TomSelect !== 'undefined') {
                        new window.TomSelect(selectEl, {
                            controlInput: null,
                            create: false,
                            wrapperClass: 'ts-wrapper blogs-filter-select-wrapper'
                        });
                    }
                    if (selectEl.tomselect) {
                        selectEl.tomselect.on('change', val => refine(val === 'all' ? '' : val));
                    } else {
                        selectEl.addEventListener('change', e => refine(e.target.value === 'all' ? '' : e.target.value));
                    }
                }, 500);
            }

            const helperResults = (search && search.helper && search.helper.lastResults) ? search.helper.lastResults : null;
            const currentHits = (renderOptions.results && typeof renderOptions.results.nbHits === 'number')
                ? renderOptions.results.nbHits
                : (helperResults && typeof helperResults.nbHits === 'number' ? helperResults.nbHits : null);

            if (selectEl.tomselect) {
                const ts = selectEl.tomselect;
                const refinedItem = items.find(i => i.isRefined);
                const activeValue = refinedItem ? refinedItem.value : 'all';

                let activeText = 'See all';
                if (typeof ts.clear === 'function') {
                    ts.clear(true);
                }
                ts.clearOptions();
                ts.addOption({ value: 'all', text: 'See all' });
                items.forEach(item => {
                    const count = (item.isRefined && typeof currentHits === 'number') ? currentHits : item.count;
                    const text = `${item.label} (${count})`;
                    if (item.value === activeValue) {
                        activeText = text;
                    }
                    ts.addOption({ value: item.value, text: text });
                });

                ts.setValue(activeValue, true);

                // Instantly update the text displayed inside the closed select box
                if (typeof ts.getItem === 'function') {
                    const itemEl = ts.getItem(activeValue);
                    if (itemEl && activeText) {
                        itemEl.textContent = activeText;
                    }
                }
            } else {
                selectEl.innerHTML = '<option value="all">See all</option>' +
                    items.map(item => {
                        const count = (item.isRefined && typeof currentHits === 'number') ? currentHits : item.count;
                        return `<option value="${item.value}" ${item.isRefined ? 'selected' : ''}>${item.label} (${count})</option>`;
                    }).join('');
            }
        }
    );

    search.addWidgets([
        instantsearch.widgets.configure({
            hitsPerPage: PAGE_SIZE
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
                            formattedDate = `${month} ${day}, ${year}`;
                        }
                    }

                    // Extract Category string (e.g. "Culture")
                    let category = '';
                    if (hit.category) {
                        if (Array.isArray(hit.category)) {
                            category = hit.category.filter(Boolean).join(', ');
                        } else if (typeof hit.category === 'string') {
                            category = hit.category;
                        }
                    }

                    // Extract Destination string (e.g. "Fiji")
                    let destination = '';
                    if (hit.destination) {
                        if (Array.isArray(hit.destination)) {
                            destination = hit.destination.filter(Boolean).join(', ');
                        } else if (typeof hit.destination === 'string') {
                            destination = hit.destination;
                        }
                    }

                    // Prioritize Category, then Destination, then fallback to 'Holidays'
                    let relatedText = '';
                    if (category && category.trim()) {
                        relatedText = category.trim() + ' ';
                    }

                    return `
                        <div class="blog-list-card d-flex flex-column h-100">
                            ${hit.image ? `
                                <a href="${articleUrl}" class="d-block text-decoration-none">
                                    <img src="${hit.image}" class="blog-list-img" alt="${hit.title || ''}" loading="lazy" />
                                </a>
                            ` : `
                                <div class="blog-list-img bg-light d-flex align-items-center justify-content-center" style="min-height: 180px;">
                                    <span class="text-muted small text-center p-2">${hit.title || ''}</span>
                                </div>
                            `}

                            <div class="blog-list-body flex-grow-1 d-flex flex-column p-4">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    ${category ? `
                                        <span class="blog-list-tag m-0 fw-bold" style="font-size:10px; text-transform:uppercase;">
                                            ${category}
                                        </span>
                                    ` : ''}

                                    ${formattedDate ? `
                                        <span class="text-muted ms-auto" style="font-size:11px;">
                                            ${formattedDate}
                                        </span>
                                    ` : ''}
                                </div>

                                <a href="${articleUrl}" class="text-decoration-none text-dark d-flex flex-column flex-grow-1">
                                    <h3 class="blog-list-title mb-2 fs-5 fw-bold">
                                        ${hit.title || ''}
                                    </h3>

                                    ${hit.summary ? `
                                        <p class="text-muted small mb-3 flex-grow-1" style="display:-webkit-box; -webkit-line-clamp:2; -webkit-box-orient:vertical; overflow:hidden;">
                                            ${hit.summary}
                                        </p>
                                    ` : ''}
                                </a>

                                <span class="text-pink fw-bold mt-auto" style="font-size:12px; letter-spacing:0.5px;">
                                    Related: ${relatedText}Holidays
                                </span>
                            </div>
                        </div>`;
                },
                empty: `<div class="col-12 text-center py-5 text-muted" style="font-size: 1.1rem;">No results found.</div>`
            }
        }),

        instantsearch.widgets.pagination({
            container: '#algolia-pagination',
            cssClasses: {
                root: 'etg-pagination-algolia d-flex justify-content-center',
                list: 'pagination m-0',
                item: 'page-item',
                link: 'page-link',
                selectedItem: 'active',
                disabledItem: 'disabled'
            },
            templates: { previous: '‹', next: '›' },
            showFirst: false, showLast: false
        }),

        {
            init() { },
            render(opts) {
                if (!opts.results) return;

                const hasDisjunctive = Object.values(opts.helper.state.disjunctiveFacetsRefinements || {}).some(r => r.length > 0);
                const hasConjunctive = Object.values(opts.helper.state.facetsRefinements || {}).some(r => r.length > 0);
                const hasHierarchical = Object.values(opts.helper.state.hierarchicalFacetsRefinements || {}).some(r => r.length > 0);

                const active = (opts.helper.state.query || '') !== '' || hasDisjunctive || hasConjunctive || hasHierarchical;

                if (active !== isAlgoliaActive) {
                    isAlgoliaActive = active;
                    document.getElementById('server-grid').style.display = active ? 'none' : '';
                    document.getElementById('server-pagination').style.display = active ? 'none' : '';
                    document.getElementById('algolia-results').style.display = active ? '' : 'none';
                }
                const countEl = document.getElementById('algolia-stats-count');

                // Use the constant we pulled from the data attribute!
                if (countEl) countEl.textContent = active ? opts.results.nbHits : TOTAL_ARTICLES;

                const paginationEl = document.getElementById('algolia-pagination');
                if (paginationEl) {
                    const shouldShow = active && (opts.results.nbHits > PAGE_SIZE);
                    paginationEl.style.display = shouldShow ? '' : 'none';
                    if (paginationEl.parentElement) {
                        paginationEl.parentElement.style.display = shouldShow ? '' : 'none';
                    }
                }
            }
        }
    ]);

    search.start();
})();
