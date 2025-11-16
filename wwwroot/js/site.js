// wwwroot/js/site.js

// Runs after DOM is ready
window.addEventListener('DOMContentLoaded', () => {

    document.addEventListener('click', async (event) => {

        // --------------------------------------------------
        // 1) Photo time hint button
        // --------------------------------------------------
        const photoBtn = event.target.closest('.gl-photo-hint');
        if (photoBtn) {
            const place = photoBtn.dataset.place || '';
            const city = photoBtn.dataset.city || 'Cincinnati';

            if (!place) {
                alert('No place name found for this card.');
                return;
            }

            const url = `/api/photo-time-hint?place=${encodeURIComponent(place)}&city=${encodeURIComponent(city)}`;

            try {
                const resp = await fetch(url);
                if (!resp.ok) {
                    alert('Could not fetch photo time hint right now.');
                    return;
                }

                const data = await resp.json();

                if (data.error) {
                    alert(data.error);
                    return;
                }

                if (data.sunset) {
                    alert(`Sunset time near ${data.place || place}:\n${data.sunset}`);
                } else {
                    alert('No sunset data available for this location.');
                }
            } catch (err) {
                console.error(err);
                alert('There was a problem contacting the photo time service.');
            }

            return; // We handled this click
        }

        // --------------------------------------------------
        // 2) About this place (Wikipedia summary → modal)
        // --------------------------------------------------
        const aboutBtn = event.target.closest('.gl-about-place');
        if (aboutBtn) {
            const title = aboutBtn.dataset.title || '';
            const lang = aboutBtn.dataset.lang || 'en';

            if (!title) {
                alert('No title found for this place.');
                return;
            }

            const url = `/api/about-place?title=${encodeURIComponent(title)}&lang=${encodeURIComponent(lang)}`;

            try {
                const resp = await fetch(url);
                if (!resp.ok) {
                    alert('Could not fetch information about this place right now.');
                    return;
                }

                const data = await resp.json();
                console.log('About-place data:', data);

                if (data.error) {
                    alert(data.error);
                    return;
                }

                // Grab modal elements
                const modalEl = document.getElementById('aboutPlaceModal');
                const titleEl = document.getElementById('aboutPlaceModalLabel');
                const extractEl = document.getElementById('aboutPlaceModalExtract');
                const readMoreEl = document.getElementById('aboutPlaceReadMore');
                const sourceEl = document.getElementById('aboutPlaceModalSource');

                if (!modalEl || !titleEl || !extractEl || !readMoreEl || !sourceEl) {
                    console.error('AboutPlace modal elements not found in DOM.');
                    alert('There was a problem displaying details for this place.');
                    return;
                }

                // Fill modal content
                titleEl.textContent = data.title || title;

                // Replace double newlines with <br><br> so paragraphs are preserved
                const extract = (data.extract || '').replace(/\n\n/g, '<br><br>');
                extractEl.innerHTML = extract || 'No additional information available.';

                // Footer text
                sourceEl.textContent = 'Source: Wikipedia';

                // Configure "Read more" link - NOTE: using pageUrl from API
                if (data.pageUrl) {
                    readMoreEl.href = data.pageUrl;
                    readMoreEl.classList.remove('d-none');
                } else {
                    readMoreEl.classList.add('d-none');
                }

                // Show the Bootstrap modal
                const bsModal = new bootstrap.Modal(modalEl);
                bsModal.show();

            } catch (err) {
                console.error(err);
                alert('There was a problem contacting the Wikipedia service.');
            }
        }

    });

});
