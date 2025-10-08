(function () {
    const TAGS = {
        "LDR": "Leader", "001": "Control Number", "005": "Date/Time of Latest Transaction",
        "006": "Additional Material Characteristics", "007": "Physical Description Fixed Field",
        "008": "Fixed-Length Data Elements", "010": "LCCN", "016": "National Bibliographic Agency Control Number",
        "017": "Copyright Registration Number", "020": "ISBN", "022": "ISSN",
        "024": "Other Standard Identifier", "035": "System Control Number", "040": "Cataloging Source",
        "041": "Language Code", "050": "LC Call Number", "082": "Dewey Number",
        "100": "Main Entry—Personal Name", "110": "Main Entry—Corporate Name",
        "111": "Main Entry—Meeting Name", "130": "Uniform Title", "240": "Uniform Title",
        "245": "Title Statement", "246": "Varying Form of Title", "250": "Edition Statement",
        "260": "Publication, Distribution, etc.", "264": "Production/Publication/Distribution/Manufacture/Copyright",
        "300": "Physical Description", "336": "Content Type", "337": "Media Type", "338": "Carrier Type",
        "490": "Series Statement", "500": "General Note", "504": "Bibliography Note", "505": "Formatted Contents Note",
        "520": "Summary", "538": "System Details Note", "546": "Language Note",
        "600": "Subject Added Entry—Personal", "610": "Subject Added Entry—Corporate",
        "611": "Subject Added Entry—Meeting", "630": "Subject Added Entry—Uniform Title",
        "650": "Subject Added Entry—Topical", "651": "Subject Added Entry—Geographic", "655": "Index Term—Genre/Form",
        "700": "Added Entry—Personal", "710": "Added Entry—Corporate", "711": "Added Entry—Meeting",
        "730": "Added Entry—Uniform Title", "740": "Added Entry—Uncontrolled Title",
        "776": "Additional Physical Form", "780": "Preceding Entry", "785": "Succeeding Entry",
        "830": "Series Added Entry—Uniform Title", "852": "Location", "856": "Electronic Location and Access",
        "880": "Alternate Graphic Representation", "886": "Foreign MARC"
    };

    const SUBF = {
        "010": { a: "LCCN" },
        "020": { a: "ISBN", c: "Terms of availability", q: "Qualifying information", z: "Canceled/invalid ISBN", "6": "Linkage", "8": "Field link and sequence number" },
        "022": { a: "ISSN", l: "ISSN-L", m: "Canceled ISSN-L", y: "Incorrect ISSN", z: "Canceled ISSN", "2": "Source", "6": "Linkage", "8": "Field link and sequence number" },
        "035": { a: "System control number", z: "Canceled/invalid control number", "6": "Linkage", "8": "Field link and sequence number" },
        "040": { a: "Original cataloging agency", b: "Language of cataloging", e: "Description conventions", c: "Transcribing agency", d: "Modifying agency", f: "Subject heading/thesaurus conventions", g: "Other conventions", "6": "Linkage", "8": "Field link and sequence number" },
        "041": { a: "Language code of text/sound track", b: "Language code of summary/abstract", h: "Original language" },
        "050": { a: "Classification number", b: "Item number", "3": "Materials specified" },
        "082": { a: "Classification number", b: "Item number", "2": "Edition number" },
        "100": { a: "Personal name", b: "Numeration", c: "Titles and other words", d: "Dates", e: "Relator term", f: "Date of a work", g: "Miscellaneous information", q: "Fuller form of name", u: "Affiliation", 0: "Authority record control number", 1: "Real world object URI", 4: "Relationship", 6: "Linkage", 8: "Field link and sequence number" },
        "110": { a: "Corporate name", b: "Subordinate unit", e: "Relator term" },
        "111": { a: "Meeting name", c: "Location of meeting", d: "Date of meeting", e: "Subordinate unit" },
        "245": { a: "Title", b: "Remainder of title", c: "Statement of responsibility", f: "Inclusive dates", g: "Bulk dates", h: "Medium", k: "Form", l: "Language", n: "Number of part/section", p: "Name of part/section", s: "Version", "6": "Linkage", "8": "Field link and sequence number" },
        "246": { a: "Varying form of title", b: "Remainder of title", i: "Display text", n: "Number of part/section", p: "Name of part/section" },
        "250": { a: "Edition statement", b: "Remainder of edition statement" },
        "260": { a: "Place of publication", b: "Publisher", c: "Date of publication", e: "Place of manufacture", f: "Manufacturer", g: "Date of manufacture", "3": "Materials specified" },
        "264": { a: "Place", b: "Publisher", c: "Date", "3": "Materials specified" },
        "300": { a: "Extent", b: "Other physical details", c: "Dimensions", e: "Accompanying material", f: "Type of unit", g: "Size", 3: "Materials specified" },
        "336": { a: "Content type term", b: "Content type code", "2": "Source" },
        "337": { a: "Media type term", b: "Media type code", "2": "Source" },
        "338": { a: "Carrier type term", b: "Carrier type code", "2": "Source" },
        "490": { a: "Series statement", v: "Volume/sequential designation", x: "ISSN" },
        "500": { a: "Note" },
        "504": { a: "Bibliography, etc. note" },
        "505": { a: "Formatted contents", t: "Title of a work", r: "Statement of responsibility", g: "Miscellaneous information" },
        "520": { a: "Summary, etc." },
        "538": { a: "System details" },
        "546": { a: "Language note" },
        "600": { a: "Personal name", d: "Dates", q: "Fuller form", x: "Topical subdivision", y: "Chronological subdivision", z: "Geographic subdivision", "2": "Source" },
        "610": { a: "Corporate name", b: "Subordinate unit", x: "Topical subdivision", y: "Chronological subdivision", z: "Geographic subdivision", "2": "Source" },
        "611": { a: "Meeting name", c: "Location", d: "Date", x: "Topical subdivision", y: "Chronological subdivision", z: "Geographic subdivision", "2": "Source" },
        "630": { a: "Uniform title", x: "Topical subdivision", y: "Chronological subdivision", z: "Geographic subdivision", "2": "Source" },
        "650": { a: "Topical term", v: "Form subdivision", x: "Topical subdivision", y: "Chronological subdivision", z: "Geographic subdivision", "2": "Source" },
        "651": { a: "Geographic name", v: "Form subdivision", x: "Topical subdivision", y: "Chronological subdivision", z: "Geographic subdivision", "2": "Source" },
        "655": { a: "Genre/form term", "2": "Source of term" },
        "700": { a: "Personal name", b: "Numeration", c: "Titles", d: "Dates", e: "Relator term", f: "Date of a work", g: "Miscellaneous information", i: "Relationship information", j: "Attribution qualifier", k: "Form subheading", l: "Language of a work", n: "Number of part/section", p: "Name of part/section", q: "Fuller form", t: "Title of work", u: "Affiliation", 0: "Authority record control number", 1: "Real world object URI", 4: "Relationship", 6: "Linkage", 8: "Field link and sequence number" },
        "710": { a: "Corporate name", b: "Subordinate unit", e: "Relator term", t: "Title of work" },
        "711": { a: "Meeting name", c: "Location", d: "Date", e: "Subordinate unit", t: "Title of work" },
        "730": { a: "Uniform title" },
        "740": { a: "Uncontrolled related/analytical title" },
        "776": { i: "Display text", t: "Title", w: "Record control number", z: "ISBN" },
        "780": { t: "Title", w: "Record control number" },
        "785": { t: "Title", w: "Record control number" },
        "830": { a: "Series added entry—uniform title", v: "Volume/sequential designation" },
        "852": { a: "Location", b: "Sublocation or collection", c: "Shelving location", d: "Former shelving location", e: "Address", f: "Coded location qualifier", g: "Non-coded location qualifier", h: "Classification part", i: "Item part", j: "Shelving control number", k: "Call number prefix", l: "Shelving form of title", m: "Call number suffix", n: "Country code", p: "Piece designation", q: "Piece physical condition", t: "Copy number", u: "Uniform Resource Identifier", x: "Nonpublic note", z: "Public note", "2": "Source of classification or shelving scheme", "3": "Materials specified", "6": "Linkage", "8": "Field link and sequence number" },
        "856": { u: "URL", y: "Link text", z: "Public note", "2": "Access method", "3": "Materials specified", q: "Electronic format type", w: "Record control number", x: "Nonpublic note", "4": "Relationship", "6": "Linkage", "8": "Field link and sequence number" }
    };
    const INDS = {
        "245": {
            "1": { "0": "No added entry", "1": "Added entry", " ": "No information" },
            "2": { "0": "0 nonfiling characters", "1": "1 nonfiling character", "2": "2 nonfiling characters", "3": "3 nonfiling characters", "4": "4 nonfiling characters", "5": "5 nonfiling characters", "6": "6 nonfiling characters", "7": "7 nonfiling characters", "8": "8 nonfiling characters", "9": "9 nonfiling characters", " ": "No nonfiling characters specified" }
        },
        "100": { "1": { "0": "Forename", "1": "Surname", "3": "Family name", " ": "Not applicable" } },
        "700": {
            "1": { "0": "Forename", "1": "Surname", "3": "Family name", " ": "Not applicable" },
            "2": { " ": "No information", "2": "Analytical entry" }
        },
        "264": {
            "1": { "0": "Not applicable/No information", "1": "Primary", "2": "Intervening", "3": "Current/Latest" },
            "2": { "0": "Production", "1": "Publication", "2": "Distribution", "3": "Manufacture", "4": "Copyright notice date", " ": "No information" }
        },
        "650": { "2": { "0": "LCSH", "1": "LC children’s", "2": "MeSH", "3": "NAL", "4": "Source not specified", "5": "Canadian Subject Headings", "6": "RVM", "7": "Source in $2", " ": "No information" } },
        "856": {
            "1": { "0": "Email", "1": "FTP", "2": "Remote login (Telnet)", "3": "Dial-up", "4": "HTTP", "7": "Method in $2", " ": "No information" },
            "2": { "0": "Resource", "1": "Version of resource", "2": "Related resource", "8": "No display constant", " ": "No information" }
        },
        "041": {
            "1": { "0": "Item not a translation", "1": "Item is or includes translation", " ": "No information" },
            "2": { " ": "MARC code", "7": "Source in $2" }
        },
        "082": {
            "1": { "0": "Full edition", "1": "Abridged" },
            "2": { "0": "LC assigned", "4": "Assigned by other agency" }
        }
    };

    function setTitle(el, text) { if (!el.getAttribute('title')) el.setAttribute('title', text); }

    function initTooltips(root) {
        root.querySelectorAll('.t[data-marc-tag]').forEach(el => {
            const tag = el.dataset.marcTag;
            setTitle(el, TAGS[tag] ? `${tag} — ${TAGS[tag]}` : tag);
        });
        root.querySelectorAll('.sf[data-marc-tag][data-marc-code]').forEach(el => {
            const tag = el.dataset.marcTag, code = el.dataset.marcCode;
            const desc = (SUBF[tag] && SUBF[tag][code]) || "";
            setTitle(el, desc ? `${tag} $${code} — ${desc}` : `${tag} $${code}`);
        });
        root.querySelectorAll('.i[data-marc-tag][data-ind-pos]').forEach(el => {
            const tag = el.dataset.marcTag, pos = el.dataset.indPos, raw = el.dataset.indVal || " ";
            const val = raw === " " ? "(blank)" : raw;
            const map = (INDS[tag] && INDS[tag][pos]) || null;
            const label = map ? (map[raw] || map[" "] || "") : "";
            setTitle(el, label ? `Ind${pos}: ${val} — ${label}` : `Ind${pos}: ${val}`);
        });
    }
    window.initTooltips = initTooltips;
    document.addEventListener('DOMContentLoaded', () => initTooltips(document));
})();
(function () {
    const ACTION_LABELS = {
        KeepLeft: 'Kept left record',
        KeepRight: 'Kept right record',
        NotDuplicate: 'Marked as not duplicate',
        Skip: 'Skipped pair'
    };
    const STORAGE_KEY = 'bibDedupe:lastAction';
    const TOAST_MAX_AGE = 5 * 60 * 1000;
    const INITIAL_DISABLE_MS = 1500;

    const page = document.querySelector('.page');
    const toastContainer = document.querySelector('.action-toast-container');
    if (page) {
        page.classList.add('initializing');
    }

    function formatRecordLabel(title, bibIdValue) {
        const trimmedTitle = (title || '').trim();
        if (trimmedTitle) {
            return trimmedTitle.length > 70 ? `${trimmedTitle.slice(0, 67)}…` : trimmedTitle;
        }
        const fallbackSource = bibIdValue ?? '';
        const fallback = (typeof fallbackSource === 'string' ? fallbackSource : String(fallbackSource)).trim();
        if (fallback) {
            return `Bib ${fallback}`;
        }
        return 'this pair';
    }

    function buildSummary(action, data) {
        const actionLabel = ACTION_LABELS[action] || action;
        const leftLabel = formatRecordLabel(data.leftTitle, data.leftBibId);
        const rightLabel = formatRecordLabel(data.rightTitle, data.rightBibId);
        return `${actionLabel}: ${leftLabel} vs ${rightLabel}`;
    }

    function rememberLastAction(payload) {
        try {
            const data = {
                action: payload.action,
                leftBibId: payload.leftBibId,
                rightBibId: payload.rightBibId,
                leftTitle: payload.leftTitle || '',
                rightTitle: payload.rightTitle || '',
                reviewUrl: payload.reviewUrl || '',
                timestamp: Date.now()
            };
            sessionStorage.setItem(STORAGE_KEY, JSON.stringify(data));
        } catch (err) {
            console.warn('Unable to persist last action toast payload.', err);
        }
    }

    function loadLastAction() {
        try {
            const raw = sessionStorage.getItem(STORAGE_KEY);
            if (!raw) {
                return null;
            }
            sessionStorage.removeItem(STORAGE_KEY);
            const data = JSON.parse(raw);
            if (!data || !data.action) {
                return null;
            }
            if (typeof data.timestamp === 'number' && Date.now() - data.timestamp > TOAST_MAX_AGE) {
                return null;
            }
            return data;
        } catch (err) {
            console.warn('Unable to load last action toast payload.', err);
            return null;
        }
    }

    function showToast(data) {
        if (!toastContainer) {
            return;
        }
        const summary = buildSummary(data.action, data);
        if (!summary) {
            return;
        }
        const toast = document.createElement('div');
        toast.className = 'action-toast';
        toast.setAttribute('role', 'status');

        const message = document.createElement('div');
        message.className = 'action-toast__message';
        message.textContent = summary;
        toast.appendChild(message);

        const actions = document.createElement('div');
        actions.className = 'action-toast__actions';

        const reviewButton = document.createElement('button');
        reviewButton.type = 'button';
        reviewButton.className = 'action-toast__button';
        reviewButton.textContent = 'Review again';
        if (data.reviewUrl) {
            reviewButton.addEventListener('click', () => {
                clearTimeout(hideTimer);
                window.location.href = data.reviewUrl;
            });
        } else {
            reviewButton.disabled = true;
        }
        actions.appendChild(reviewButton);

        const dismissButton = document.createElement('button');
        dismissButton.type = 'button';
        dismissButton.className = 'action-toast__dismiss';
        dismissButton.setAttribute('aria-label', 'Dismiss notification');
        dismissButton.textContent = '×';
        actions.appendChild(dismissButton);

        toast.appendChild(actions);
        toastContainer.appendChild(toast);

        const hideToast = () => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 200);
        };

        let hideTimer = setTimeout(hideToast, 6000);

        dismissButton.addEventListener('click', () => {
            clearTimeout(hideTimer);
            hideToast();
        });

        requestAnimationFrame(() => toast.classList.add('show'));
    }

    const storedAction = loadLastAction();
    if (storedAction) {
        showToast(storedAction);
    }

    const tokenInput = document.querySelector('#token-form input[name="__RequestVerificationToken"]');
    const token = tokenInput ? tokenInput.value : '';
    const url = page ? page.dataset.resolveUrl : '';
    const leftBibIdValue = page ? (page.dataset.leftBibId || '') : '';
    const rightBibIdValue = page ? (page.dataset.rightBibId || '') : '';
    const leftBibId = parseInt(leftBibIdValue, 10);
    const rightBibId = parseInt(rightBibIdValue, 10);
    const reviewUrl = page ? page.dataset.reviewUrl : '';
    const leftTitle = page ? page.dataset.leftTitle : '';
    const rightTitle = page ? page.dataset.rightTitle : '';

    const actionButtons = Array.from(document.querySelectorAll('.controls button'));
    if (actionButtons.length) {
        actionButtons.forEach(btn => {
            btn.disabled = true;
        });
        setTimeout(() => {
            actionButtons.forEach(btn => {
                if (btn.dataset.locked === 'true') {
                    return;
                }
                btn.disabled = false;
            });
        }, INITIAL_DISABLE_MS);
    }

    function setButtonsLocked(isLocked) {
        actionButtons.forEach(btn => {
            if (isLocked) {
                btn.dataset.locked = 'true';
                btn.disabled = true;
            } else {
                delete btn.dataset.locked;
                btn.disabled = false;
            }
        });
    }

    actionButtons.forEach(btn => {
        btn.addEventListener('click', async () => {
            if (btn.disabled) {
                return;
            }
            if (!url || !token) {
                console.error('Resolve endpoint is not configured.');
                return;
            }
            const body = new URLSearchParams({
                action: btn.dataset.action,
                leftBibId: leftBibIdValue,
                rightBibId: rightBibIdValue
            });
            const currentPairUrl = window.location.href;
            try {
                setButtonsLocked(true);
                const response = await fetch(url, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest',
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: body.toString()
                });
                const data = await response.json().catch(() => ({}));
                if (!response.ok) {
                    const message = data.error || 'Unable to save this decision. Remove any conflicting merges and try again.';
                    window.alert(message);
                    setButtonsLocked(false);
                    return;
                }
                const badge = document.querySelector('.menu .badge');
                if (badge) {
                    badge.textContent = (parseInt(badge.textContent || '0', 10) + 1).toString();
                }
                const currentPairControl = document.querySelector('.menu [data-current-pair]');
                if (currentPairControl && currentPairControl.tagName === 'A') {
                    const disabled = document.createElement('span');
                    disabled.className = 'disabled';
                    disabled.dataset.currentPair = 'true';
                    disabled.setAttribute('aria-disabled', 'true');
                    disabled.textContent = currentPairControl.textContent;
                    currentPairControl.replaceWith(disabled);
                }
                rememberLastAction({
                    action: btn.dataset.action,
                    leftBibId: Number.isNaN(leftBibId) ? leftBibIdValue : leftBibId,
                    rightBibId: Number.isNaN(rightBibId) ? rightBibIdValue : rightBibId,
                    leftTitle,
                    rightTitle,
                    reviewUrl: currentPairUrl
                });
                const nextUrl = data.nextPairUrl || reviewUrl || window.location.href;
                window.location.href = nextUrl;
            } catch (err) {
                console.error(err);
                setButtonsLocked(false);
            }
        });
    });

    const leftDiv = document.getElementById('leftDiv');
    const rightDiv = document.getElementById('rightDiv');

    function collectMap(tbody) {
        const map = {};
        tbody.querySelectorAll('tr').forEach(row => {
            const tagEl = row.querySelector('.t');
            const tag = tagEl && tagEl.dataset.marcTag;
            if (!tag) return; // skip rows without a tag
            (map[tag] ||= []).push(row);
        });
        return map;
    }

    function createPlaceholder(tag) {
        const tr = document.createElement('tr');
        tr.classList.add('placeholder-row');
        tr.dataset.placeholder = 'true';
        tr.setAttribute('aria-hidden', 'true');
        tr.innerHTML =
            `<td class="tag"><span class="t" data-marc-tag="${tag}">${tag}</span></td>` +
            `<td class="ind"><span class="i" data-marc-tag="${tag}" data-ind-pos="1" data-ind-val=" ">&nbsp;</span></td>` +
            `<td class="ind"><span class="i" data-marc-tag="${tag}" data-ind-pos="2" data-ind-val=" ">&nbsp;</span></td>` +
            `<td class="data"><div class="wrap"></div></td>`;
        return tr;
    }

    function alignTables() {
        const leftBody = leftDiv.querySelector('tbody');
        const rightBody = rightDiv.querySelector('tbody');
        if (!leftBody || !rightBody) return;

        const leftMap = collectMap(leftBody);
        const rightMap = collectMap(rightBody);

        const tags = Array.from(new Set([...Object.keys(leftMap), ...Object.keys(rightMap)]))
            .sort((a, b) => {
                if (a === 'LDR') return b === 'LDR' ? 0 : -1;
                if (b === 'LDR') return 1;
                return parseInt(a, 10) - parseInt(b, 10);
            });

        leftBody.innerHTML = '';
        rightBody.innerHTML = '';

        tags.forEach(tag => {
            const lrows = leftMap[tag] || [];
            const rrows = rightMap[tag] || [];
            const max = Math.max(lrows.length, rrows.length);
            for (let i = 0; i < max; i++) {
                leftBody.appendChild(lrows[i] || createPlaceholder(tag));
                rightBody.appendChild(rrows[i] || createPlaceholder(tag));
            }
        });
        if (typeof initTooltips === 'function') {
            initTooltips(leftDiv);
            initTooltips(rightDiv);
        }
        syncRowHeights();
    }

    function syncRowHeights() {
        const leftRows = leftDiv.querySelectorAll('tbody tr');
        const rightRows = rightDiv.querySelectorAll('tbody tr');
        const len = Math.max(leftRows.length, rightRows.length);
        for (let i = 0; i < len; i++) {
            const l = leftRows[i];
            const r = rightRows[i];
            if (!l || !r) continue;
            l.style.height = r.style.height = 'auto';
            const h = Math.max(l.offsetHeight, r.offsetHeight);
            l.style.height = r.style.height = `${h}px`;
        }
    }

    function bindScrollSync(a, b) {
        a.addEventListener('scroll', () => {
            if (a.__syncing) { a.__syncing = false; return; }
            b.__syncing = true;
            b.scrollTop = a.scrollTop;
            b.scrollLeft = a.scrollLeft;
        });
    }

    function initTabs() {
        document.querySelectorAll('.frame').forEach(frame => {
            const buttons = frame.querySelectorAll('.tab-buttons .tab-button');
            const panes = frame.querySelectorAll('.tab-pane');
            buttons.forEach(btn => {
                btn.addEventListener('click', () => {
                    const target = btn.dataset.tab;
                    buttons.forEach(b => b.classList.toggle('active', b === btn));
                    panes.forEach(p => p.classList.toggle('active', p.dataset.tab === target));
                    if (target.endsWith('marc')) {
                        syncRowHeights();
                    }
                });
            });
        });
    }

    alignTables();
    bindScrollSync(leftDiv, rightDiv);
    bindScrollSync(rightDiv, leftDiv);
    window.addEventListener('resize', syncRowHeights);
    initTabs();
    if (page) {
        requestAnimationFrame(() => page.classList.remove('initializing'));
    }
})();
