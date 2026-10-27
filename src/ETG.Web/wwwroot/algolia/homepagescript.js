(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('heroSearchForm');
        if (!form) return;

        var appId = form.dataset.appId;
        var apiKey = form.dataset.apiKey;
        var indexName = form.dataset.indexName;

        if (!appId || !apiKey || !indexName) return;

        var searchClient = algoliasearch(appId, apiKey);
        var index = searchClient.initIndex(indexName);

        var destSelect = document.getElementById('heroDestination');
        var expSelect = document.getElementById('heroExperience');

        var masterDestinations = [];
        var masterExperiences = [];
        var destExpCache = {};
        var expDestCache = {};
        var isUpdating = false;

        // Dynamic formatting helper: converts PascalCase / unspaced names to formatted display names with spaces
        // e.g., 'NewCaledonia' -> 'New Caledonia', 'CookIslands' -> 'Cook Islands', 'CityStays' -> 'City Stays'
        function formatDisplayName(str) {
            if (!str || str === 'all') return str;
            var clean = str.replace(/-[a-z0-9]{8}$/i, '');
            clean = clean.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
            clean = clean.replace(/\s*&\s*/g, ' & ');
            return clean.replace(/\s+/g, ' ').trim();
        }

        // Dynamically expands facet filter to match spaced and unspaced values in Algolia
        function expandFacetFilter(attr, val) {
            var raw = val || '';
            var noSpaces = raw.replace(/\s+/g, '');
            var withSpaces = formatDisplayName(raw);
            var variants = [raw];
            if (noSpaces && variants.indexOf(noSpaces) === -1) variants.push(noSpaces);
            if (withSpaces && variants.indexOf(withSpaces) === -1) variants.push(withSpaces);
            return variants.map(function(v) { return attr + ':' + v; });
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

        function updateTomSelectDropdown(selectEl, list, defaultText, activeValue) {
            if (!selectEl) return;

            var allowedMap = {};
            allowedMap['all'] = true;
            var html = '<option value="all">' + defaultText + '</option>';
            if (list && Array.isArray(list)) {
                list.forEach(function (name) {
                    var val = formatDisplayName(name);
                    allowedMap[val] = true;
                    var isSelected = (val === activeValue || name === activeValue);
                    html += '<option value="' + val + '"' + (isSelected ? ' selected' : '') + '>' + val + '</option>';
                });
            }
            selectEl.innerHTML = html;

            var formattedActive = formatDisplayName(activeValue);
            var targetVal = (formattedActive && allowedMap[formattedActive]) ? formattedActive : 'all';

            if (selectEl.tomselect) {
                var ts = selectEl.tomselect;
                isUpdating = true;
                ts.clear(true);
                ts.clearOptions();
                ts.addOption({ value: 'all', text: defaultText });
                if (list && Array.isArray(list)) {
                    list.forEach(function (name) {
                        var val = formatDisplayName(name);
                        ts.addOption({ value: val, text: val });
                    });
                }
                ts.refreshOptions(false);
                ts.setValue(targetVal, true);
                isUpdating = false;
            } else if (window.TomSelect) {
                var ts = new TomSelect(selectEl, { controlInput: null, maxOptions: 1000 });
                attachTomSelectEvents(selectEl, ts);
            }
        }

        function attachTomSelectEvents(selectEl, ts) {
            if (!ts) return;
            if (selectEl.id === 'heroDestination') {
                ts.on('change', function (val) {
                    if (isUpdating) return;
                    onDestinationChange(val);
                });
            } else if (selectEl.id === 'heroExperience') {
                ts.on('change', function (val) {
                    if (isUpdating) return;
                    onExperienceChange(val);
                });
            }
        }

        if (destSelect) {
            destSelect.addEventListener('change', function (e) {
                if (isUpdating) return;
                onDestinationChange(e.target.value);
            });
        }
        if (expSelect) {
            expSelect.addEventListener('change', function (e) {
                if (isUpdating) return;
                onExperienceChange(e.target.value);
            });
        }

        function onDestinationChange(destVal) {
            var currentExp = expSelect && expSelect.tomselect ? expSelect.tomselect.getValue() : (expSelect ? expSelect.value : '');
            if (destVal && destVal !== 'all') {
                var formattedDest = formatDisplayName(destVal);
                if (destExpCache[formattedDest]) {
                    applyExperiences(destExpCache[formattedDest], currentExp);
                } else {
                    index.search('', { facetFilters: [expandFacetFilter('Destinations', destVal)], facets: ['Experiences'], hitsPerPage: 0 })
                        .then(function (res) {
                            var validExp = Object.keys((res.facets && res.facets['Experiences']) || {}).sort();
                            destExpCache[formattedDest] = validExp;
                            applyExperiences(validExp, currentExp);
                        })
                        .catch(function (err) {
                            console.warn('Error fetching experiences for ' + destVal, err);
                        });
                }
            } else {
                // Destination reset to 'all'
                if (currentExp && currentExp !== 'all') {
                    applyExperiences(masterExperiences, currentExp);
                } else {
                    updateTomSelectDropdown(expSelect, masterExperiences, 'Select', 'all');
                    updateTomSelectDropdown(destSelect, masterDestinations, 'Select', 'all');
                }
            }
        }

        function applyExperiences(validExp, currentExp) {
            var keepExp = (validExp && listContains(validExp, currentExp)) ? formatDisplayName(currentExp) : 'all';
            updateTomSelectDropdown(expSelect, validExp, 'Select', keepExp);
        }

        function onExperienceChange(expVal) {
            var currentDest = destSelect && destSelect.tomselect ? destSelect.tomselect.getValue() : (destSelect ? destSelect.value : '');
            if (expVal && expVal !== 'all') {
                var formattedExp = formatDisplayName(expVal);
                if (expDestCache[formattedExp]) {
                    applyDestinations(expDestCache[formattedExp], currentDest);
                } else {
                    index.search('', { facetFilters: [expandFacetFilter('Experiences', expVal)], facets: ['Destinations'], hitsPerPage: 0 })
                        .then(function (res) {
                            var validDest = Object.keys((res.facets && res.facets['Destinations']) || {}).sort();
                            expDestCache[formattedExp] = validDest;
                            applyDestinations(validDest, currentDest);
                        })
                        .catch(function (err) {
                            console.warn('Error fetching destinations for ' + expVal, err);
                        });
                }
            } else {
                // Experience reset to 'all'
                var curDest = destSelect && destSelect.tomselect ? destSelect.tomselect.getValue() : (destSelect ? destSelect.value : '');
                if (curDest && curDest !== 'all') {
                    applyDestinations(masterDestinations, curDest);
                } else {
                    updateTomSelectDropdown(destSelect, masterDestinations, 'Select', 'all');
                    updateTomSelectDropdown(expSelect, masterExperiences, 'Select', 'all');
                }
            }
        }

        function applyDestinations(validDest, currentDest) {
            var keepDest = (validDest && listContains(validDest, currentDest)) ? formatDisplayName(currentDest) : 'all';
            updateTomSelectDropdown(destSelect, validDest, 'Select', keepDest);
        }

        // 1. Fetch BOTH Destinations and Experiences directly from Algolia
        index.search('', { facets: ['Destinations', 'Experiences'], hitsPerPage: 0 })
            .then(function (result) {
                var destFacets = result.facets && result.facets['Destinations'];
                var expFacets = result.facets && result.facets['Experiences'];

                masterDestinations = destFacets ? Object.keys(destFacets).sort() : [];
                masterExperiences = expFacets ? Object.keys(expFacets).sort() : [];

                updateTomSelectDropdown(destSelect, masterDestinations, 'Select', 'all');
                updateTomSelectDropdown(expSelect, masterExperiences, 'Select', 'all');

                if (destSelect && destSelect.tomselect) {
                    attachTomSelectEvents(destSelect, destSelect.tomselect);
                }
                if (expSelect && expSelect.tomselect) {
                    attachTomSelectEvents(expSelect, expSelect.tomselect);
                }
            })
            .catch(function (err) {
                console.error('Algolia facet error:', err);
            });

        // 2. Handle Form Submit (Redirect to Tour Landing) - Kentico 13 Query Parameters
        form.addEventListener('submit', function (event) {
            event.preventDefault();

            var destination = destSelect && destSelect.tomselect ? destSelect.tomselect.getValue() : (destSelect ? destSelect.value : '');
            var experience = expSelect && expSelect.tomselect ? expSelect.tomselect.getValue() : (expSelect ? expSelect.value : '');
            var keyword = document.getElementById('heroKeyword') ? document.getElementById('heroKeyword').value.trim() : '';

            var baseIndex = form.dataset.indexName || 'TourPricing-Live';

            var is404 = form.dataset.from === '404' ||
                        form.dataset.from === '404page' ||
                        window.location.pathname.toLowerCase().indexOf('404') !== -1 ||
                        window.location.pathname.toLowerCase().indexOf('not-found') !== -1 ||
                        document.querySelector('.error-title') !== null;
            var fromSource = is404 ? '404page' : 'homepage';

            var queryParts = [];
            queryParts.push('from=' + fromSource);

            if (keyword) {
                queryParts.push(baseIndex + '[query]=' + encodeURIComponent(keyword));
            }
            if (destination && destination !== 'all') {
                queryParts.push(baseIndex + '[refinementList][destination][0]=' + encodeURIComponent(formatDisplayName(destination)));
            }
            if (experience && experience !== 'all') {
                queryParts.push(baseIndex + '[refinementList][experience][0]=' + encodeURIComponent(formatDisplayName(experience)));
            }

            window.location.href = '/search?' + queryParts.join('&');
        });
    });
})();
