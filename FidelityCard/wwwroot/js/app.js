window.manifestInterop = {
    getManifest: async function () {
        const response = await fetch('/manifest.webmanifest?k=3');
        if (!response.ok) {
            throw new Error('Manifest not found');
        }
        return await response.json();
    },
    initDatePicker: function (elementId) {
        flatpickr("#" + elementId, {
            locale: "it",
            dateFormat: "d/m/Y",
            maxDate: "today",
            disableMobile: true,
            allowInput: true,
            onReady: function (_selectedDates, _dateStr, instance) {
                const yearInput = instance.currentYearElement;
                if (!yearInput) return;

                // Create Select
                const yearSelect = document.createElement("select");
                yearSelect.className = "cur-year";
                yearSelect.style.cssText = "color: inherit; background: transparent; border: none; font-size: inherit; font-family: inherit; font-weight: inherit; padding: 0 15px 0 5px; cursor: pointer; appearance: none; -webkit-appearance: none; -moz-appearance: none; text-align: center; width: auto; min-width: 75px; display: inline-block;";

                // Populate years (1900 to Current)
                const currentYear = new Date().getFullYear();
                for (let i = currentYear; i >= 1900; i--) {
                    const option = document.createElement("option");
                    option.value = i;
                    option.text = i;
                    yearSelect.appendChild(option);
                }

                // Initial Value
                yearSelect.value = instance.currentYear;

                // Bind Change Event
                yearSelect.addEventListener("change", function (e) {
                    instance.changeYear(parseInt(e.target.value));
                });

                // Update Select on Year Change (if changed by arrows or other means)
                instance.config.onYearChange.push(function () {
                    yearSelect.value = instance.currentYear;
                });

                // Insert and Hide Input
                yearInput.style.display = "none";
                if (yearInput.parentNode) {
                    yearInput.parentNode.insertBefore(yearSelect, yearInput);
                }
            },
            onChange: function (_selectedDates, _dateStr, instance) {
                instance.element.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
    }
};