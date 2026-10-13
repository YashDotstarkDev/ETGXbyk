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

        // 1. Fetch BOTH Destinations and Experiences directly from Algolia
        index.search('', { facets: ['Destinations', 'Experiences'], hitsPerPage: 0 })
            .then(function (result) {
                var destSelect = document.getElementById('heroDestination');
                var expSelect = document.getElementById('heroExperience');

                var destFacets = result.facets && result.facets['Destinations'];
                var expFacets = result.facets && result.facets['Experiences'];

                // Exact same TomSelect logic as tourlanding script.js
                function populateDropdown(selectEl, facetsData, defaultText) {
                    if (!selectEl) return;
                    
                    var html = '<option value="all">' + defaultText + '</option>';
                    if (facetsData) {
                        Object.keys(facetsData).sort().forEach(function (name) {
                            html += '<option value="' + name + '">' + name + '</option>';
                        });
                    }
                    selectEl.innerHTML = html;

                    if (selectEl.tomselect) {
                        selectEl.tomselect.sync();
                    } else if (window.TomSelect) {
                        new TomSelect(selectEl, { controlInput: null });
                    }
                }

                populateDropdown(destSelect, destFacets, 'Select destination');
                populateDropdown(expSelect, expFacets, 'Select experience');
            })
            .catch(function (err) {
                console.error('Algolia facet error:', err);
            });

        // 2. Handle Form Submit (Redirect to Tour Landing)
        form.addEventListener('submit', function(event) {
            event.preventDefault(); // Stop standard form submission

            var destination = document.getElementById('heroDestination').value;
            var experience = document.getElementById('heroExperience').value;
            var keyword = document.getElementById('heroKeyword').value.trim();

            var baseIndex = form.dataset.indexName;
            var searchIndex = baseIndex + '_name_asc'; 

            var params = new URLSearchParams();
            
            if (keyword) {
                params.set(searchIndex + '[query]', keyword);
            }
            if (destination && destination !== 'all') {
                params.set(searchIndex + '[menu][Destinations]', destination);
            }
            if (experience && experience !== 'all') {
                params.set(searchIndex + '[menu][Experiences]', experience);
            }

            window.location.href = '/search?' + params.toString();
        });
    });
})();
