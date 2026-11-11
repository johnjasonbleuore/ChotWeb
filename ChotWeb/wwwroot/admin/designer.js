// Site designer: edit the draft on the left, see it live on the right, publish when ready.
(function () {
    "use strict";

    var A = window.ChotAdmin, h = A.h, icon = A.icon;
    var root = document.querySelector("[data-designer]");
    if (!root) return;
    var config = JSON.parse(document.getElementById("designer-config").textContent);

    var els = {
        rail: root.querySelector("[data-rail]"),
        title: root.querySelector("[data-panel-title]"),
        hint: root.querySelector("[data-panel-hint]"),
        body: root.querySelector("[data-panel-body]"),
        stage: root.querySelector("[data-stage]"),
        device: root.querySelector("[data-device-frame]"),
        frames: Array.from(root.querySelectorAll("[data-frame]")),
        badge: root.querySelector("[data-draft-badge]"),
        status: document.querySelector("[data-save-status]"),
        publish: document.querySelector("[data-publish]"),
        discard: document.querySelector("[data-discard]"),
    };

    var state = null;          // the draft being edited
    var activePanel = "layout";
    var device = "desktop";
    var DEVICES = { desktop: 1280, tablet: 820, phone: 390 };

    // ======================================================================
    // Labels & presets
    // ======================================================================

    var SECTION_NAMES = {
        hero: "Hero — name, photo, stats",
        videos: "My Videos",
        photos: "My Photos",
        brands: "Brands I've worked with",
        contact: "Let's work together",
    };

    var SECTION_PANEL = { header: "hero", hero: "hero", videos: "videos", photos: "photos", brands: "brands", contact: "contact", footer: "contact" };
    var PANEL_SECTION = { hero: "hero", numbers: "hero", videos: "videos", photos: "photos", brands: "brands", contact: "contact" };

    var ICON_LABELS = { tiktok: "TikTok", instagram: "Instagram", youtube: "YouTube", facebook: "Facebook", mail: "Email", phone: "Phone", pin: "Location pin", heart: "Heart" };
    var BRAND_STYLE_LABELS = { glow: "Script, light", soleil: "Serif, spaced", petal: "Thin, spaced", blush: "Bold italic", honey: "Script, dark", luxe: "Pill with circle" };

    var PRESETS = [
        { name: "Peach", colors: { primary: "#FF7B22", dark: "#3A1A0B", text: "#2E1D14", surface: "#FFF4EC", background: "#FFFFFF" } },
        { name: "Blush", colors: { primary: "#EC4F8B", dark: "#4A1530", text: "#33161F", surface: "#FFF0F5", background: "#FFFFFF" } },
        { name: "Lavender", colors: { primary: "#8B5CF6", dark: "#2E1B5B", text: "#231A38", surface: "#F5F0FF", background: "#FFFFFF" } },
        { name: "Sage", colors: { primary: "#5F9E6E", dark: "#1F3A28", text: "#1E2B22", surface: "#EFF7F0", background: "#FFFFFF" } },
        { name: "Mocha", colors: { primary: "#B07A4F", dark: "#3B2416", text: "#2F2118", surface: "#F8F1EA", background: "#FFFDFB" } },
        { name: "Ocean", colors: { primary: "#1E88C8", dark: "#0F2B44", text: "#13212E", surface: "#EEF6FC", background: "#FFFFFF" } },
    ];

    var iconOptions = config.icons.map(function (i) { return { value: i, label: ICON_LABELS[i] || i }; });
    var brandStyleOptions = config.brandStyles.map(function (s) { return { value: s, label: BRAND_STYLE_LABELS[s] || s }; });

    // ======================================================================
    // Panel definitions (fields map to paths in the content JSON)
    // ======================================================================

    function text(key, label, o) { return Object.assign({ type: "text", key: key, label: label }, o); }
    function area(key, label, o) { return Object.assign({ type: "textarea", key: key, label: label }, o); }
    function link(key, label, o) { return Object.assign({ type: "url", key: key, label: label }, o); }
    function media(key, label, kind, o) { return Object.assign({ type: "media", key: key, label: label, kind: kind }, o); }
    function toggle(key, label) { return { type: "checkbox", key: key, label: label }; }
    function select(key, label, options) { return { type: "select", key: key, label: label, options: options }; }
    function group(title, fields) { return { type: "group", title: title, fields: fields }; }
    function list(key, label, o) { return Object.assign({ type: "list", key: key, label: label }, o); }

    var PANELS = [
        { id: "layout", rail: "Layout", icon: "a-layout", title: "Page layout", hint: "Drag sections to change their order. Switch off the ones you don't want.", render: renderLayout },
        { id: "theme", rail: "Theme", icon: "a-palette", title: "Colours & fonts", hint: "Pick a preset or set your own colours. Every shade on the site is made from these.", render: renderTheme },
        {
            id: "hero", rail: "Hero", icon: "a-star", title: "Header & hero", hint: "The top of your page — the first thing visitors see.",
            fields: [
                group("Header", [
                    text("header.siteName", "Site name (logo)"),
                    text("header.ctaLabel", "Collab button text", { hint: "Scrolls to your contact section." }),
                    text("header.mediaKitLabel", "Media kit button text", { hint: "Clear the text or link to hide this button." }),
                    link("header.mediaKitUrl", "Media kit link", { hint: "A web link, or mailto:you@example.com" }),
                ]),
                group("Big name", [
                    text("hero.titleLine1", "Line 1 (dark colour)"),
                    text("hero.titleLine2", "Line 2 (main colour)"),
                    text("hero.tagline", "Script tagline over the photo"),
                ]),
                group("Photo", [
                    media("hero.photoUrl", "Hero photo", "image", { hint: "A tall portrait works best (about 3:4)." }),
                ]),
                group("Intro (left)", [
                    text("hero.introTitle", "Greeting"),
                    area("hero.introText", "Intro text"),
                ]),
                group("Details (right)", [
                    text("hero.specialties", "Specialties"),
                    text("hero.location", "Location"),
                    text("hero.locationBadge", "Location badge", { placeholder: "PH" }),
                    text("hero.availability", "Availability"),
                    toggle("hero.showDecorations", "Show sparkles & hearts"),
                ]),
            ],
        },
        {
            id: "numbers", rail: "Numbers", icon: "a-chart", title: "Numbers & socials", hint: "Leave a number blank to hide it — only real numbers are shown.",
            fields: [
                list("stats", "Stats under the photo", {
                    singular: "stat", max: 4, icon: "a-chart",
                    itemTitle: function (s) { return (s.value ? s.value + " — " : "") + (s.label || "Stat"); },
                    newItem: function () { return { value: "", label: "New stat" }; },
                    fields: [text("value", "Number", { placeholder: "e.g. 250K+" }), text("label", "Label")],
                }),
                list("socials", "Social accounts", {
                    singular: "account", max: 8, icon: "a-star",
                    itemTitle: function (s) { return (s.platform || "Account") + (s.count ? " · " + s.count : ""); },
                    newItem: function () { return { platform: "TikTok", icon: "tiktok", handle: "", count: "", countLabel: "followers", url: "" }; },
                    fields: [
                        text("platform", "Platform name"),
                        select("icon", "Icon", iconOptions),
                        text("handle", "Handle", { placeholder: "@yourname" }),
                        text("count", "Followers", { placeholder: "e.g. 120K", hint: "Copy it from the app. Blank = hidden." }),
                        text("countLabel", "Followers label"),
                        link("url", "Profile link"),
                    ],
                }),
            ],
        },
        {
            id: "videos", rail: "Videos", icon: "a-video", title: "My Videos", hint: "Upload video files to play them on your site, or link to posts.",
            fields: [
                group("Heading", [
                    text("videos.titleBold", "Bold word"),
                    text("videos.titleScript", "Script word"),
                    text("videos.leftLabel", "Left label"),
                    text("videos.rightLabel", "Right label"),
                ]),
                list("videos.items", "Videos", {
                    singular: "video", max: 24, icon: "a-video",
                    thumb: function (v) { return v.thumbnailUrl || v.videoUrl; },
                    itemTitle: function (v) { return v.title || "Video"; },
                    newItem: function () { return { title: "New video", category: "", duration: "", views: "", tag: "TikTok", url: "", thumbnailUrl: "", videoUrl: "" }; },
                    fields: [
                        text("title", "Title"),
                        media("videoUrl", "Video file", "video", { hint: "Uploaded videos play in a pop-up on your site." }),
                        media("thumbnailUrl", "Cover image (optional)", "image", { hint: "Without one, the first frame of the video is shown." }),
                        link("url", "Or link to the post", { hint: "Used when there's no uploaded video." }),
                        text("tag", "Tag on the card", { placeholder: "TikTok, Reels, UGC Ad…" }),
                        text("category", "Category"),
                        text("duration", "Length", { placeholder: "0:45" }),
                        text("views", "Views", { placeholder: "e.g. 1.2M views", hint: "Blank = hidden." }),
                    ],
                }),
            ],
        },
        {
            id: "photos", rail: "Photos", icon: "a-image", title: "My Photos", hint: "The big arch photo plus your gallery tiles.",
            fields: [
                group("Heading", [
                    text("photos.titleA", "Word 1 (dark colour)"),
                    text("photos.titleB", "Word 2 (main colour)"),
                    text("photos.subtitle", "Script subtitle"),
                ]),
                group("Text", [
                    { type: "paragraphs", key: "photos.paragraphs", label: "Paragraphs", hint: "Leave an empty line between paragraphs." },
                    { type: "tags", key: "photos.tags", label: "Tags", hint: "Separate with commas." },
                    text("photos.buttonLabel", "Button text"),
                    link("photos.galleryUrl", "Button link"),
                ]),
                group("Big arch photo", [
                    media("photos.portrait.imageUrl", "Photo", "image", { hint: "Tall photo, about 1:2." }),
                    text("photos.portrait.label", "Label"),
                ]),
                list("photos.items", "Gallery photos", {
                    singular: "photo", max: 16, icon: "a-image",
                    thumb: function (p) { return p.imageUrl; },
                    itemTitle: function (p) { return p.label || "Photo"; },
                    newItem: function () { return { label: "New photo", imageUrl: "" }; },
                    fields: [media("imageUrl", "Photo", "image", { hint: "Square photos fit best." }), text("label", "Label")],
                }),
            ],
        },
        {
            id: "brands", rail: "Brands", icon: "a-tag", title: "Brands", hint: "Add brands you've worked with. Upload a logo or use a styled name.",
            fields: [
                group("Heading", [
                    text("brands.titleBold", "Bold words"),
                    text("brands.titleScript", "Script words"),
                    text("brands.leftLabel", "Left label"),
                    text("brands.rightLabel", "Right label"),
                ]),
                group("Note under the logos", [
                    toggle("brands.showNote", "Show note"),
                    text("brands.note", "Note text"),
                ]),
                list("brands.items", "Brands", {
                    singular: "brand", max: 18, icon: "a-tag",
                    thumb: function (b) { return b.logoUrl; },
                    itemTitle: function (b) { return b.name || "Brand"; },
                    newItem: function () { return { name: "New brand", style: "glow", logoUrl: "", url: "" }; },
                    fields: [
                        text("name", "Brand name"),
                        media("logoUrl", "Logo (optional)", "image", { hint: "PNG with a transparent background looks best." }),
                        select("style", "Name style (when there's no logo)", brandStyleOptions),
                        link("url", "Website (optional)"),
                    ],
                }),
            ],
        },
        {
            id: "contact", rail: "Contact", icon: "a-mail", title: "Contact & footer", hint: "How brands reach you.",
            fields: [
                group("Heading & text", [
                    text("contact.titleBold", "Bold words"),
                    text("contact.titleScript", "Script word"),
                    area("contact.text", "Text"),
                    text("contact.buttonLabel", "Button text"),
                    link("contact.buttonUrl", "Button link", { hint: "e.g. mailto:you@example.com?subject=Collab" }),
                ]),
                list("contact.items", "Contact rows", {
                    singular: "row", max: 8, icon: "a-mail",
                    itemTitle: function (c) { return c.label || "Contact"; },
                    newItem: function () { return { icon: "mail", label: "New", value: "", url: "" }; },
                    fields: [select("icon", "Icon", iconOptions), text("label", "Small label"), text("value", "Value"), link("url", "Link (optional)")],
                }),
                group("Footer", [
                    text("footer.tagline", "Tagline (also the browser tab title)"),
                    text("footer.signature", "Signature"),
                    toggle("footer.showSocialLinks", "Show social links"),
                ]),
            ],
        },
    ];

    // ======================================================================
    // State helpers
    // ======================================================================

    function getPath(obj, path) {
        return path.split(".").reduce(function (o, k) { return o === null || o === undefined ? undefined : o[k]; }, obj);
    }

    function setPath(obj, path, value) {
        var keys = path.split(".");
        var last = keys.pop();
        var target = keys.reduce(function (o, k) { if (o[k] === null || o[k] === undefined) o[k] = {}; return o[k]; }, obj);
        target[last] = value;
    }

    function validLink(v) {
        v = (v || "").trim();
        return v === "" || /^(https?:\/\/|mailto:|tel:|#|~\/)/i.test(v);
    }

    function thumbEl(url, kind) {
        var src = A.resolveUrl(url);
        return (kind || A.kindOf(url)) === "video"
            ? h("video", { src: src + "#t=0.5", muted: true, preload: "metadata", playsinline: true })
            : h("img", { src: src, alt: "" });
    }

    // ======================================================================
    // Saving & publishing
    // ======================================================================

    var saveTimer = null, saving = false, queued = false;

    function setStatus(message, isError) {
        els.status.textContent = message;
        els.status.classList.toggle("is-error", !!isError);
    }

    function setPublishState(hasChanges) {
        els.publish.disabled = !hasChanges;
        els.discard.disabled = !hasChanges;
        els.badge.hidden = !hasChanges;
    }

    function changed() {
        setStatus("Editing…");
        clearTimeout(saveTimer);
        saveTimer = setTimeout(save, 600);
    }

    async function save() {
        clearTimeout(saveTimer);
        saveTimer = null;
        if (saving) { queued = true; return; }
        saving = true;
        setStatus("Saving…");
        try {
            var res = await A.api("PUT", "draft", state);
            setPublishState(res.hasUnpublishedChanges);
            setStatus(res.hasUnpublishedChanges ? "Draft saved · not live yet" : "All changes are live");
            refreshPreview();
        } catch (err) {
            setStatus("Couldn't save", true);
            A.toast(err.message, "error");
        } finally {
            saving = false;
            if (queued) { queued = false; save(); }
        }
    }

    async function flush() {
        if (saveTimer) await save();
        while (saving) await new Promise(function (r) { setTimeout(r, 80); });
    }

    els.publish.addEventListener("click", async function () {
        els.publish.disabled = true;
        try {
            await flush();
            var res = await A.api("POST", "publish");
            setPublishState(res.hasUnpublishedChanges);
            setStatus("All changes are live");
            A.toast("Published! Visitors now see your changes.", "ok");
        } catch (err) {
            A.toast(err.message, "error");
            els.publish.disabled = false;
        }
    });

    els.discard.addEventListener("click", async function () {
        if (!confirm("Undo every change since you last published?")) return;
        try {
            clearTimeout(saveTimer); saveTimer = null;
            await flush();
            var res = await A.api("POST", "discard");
            state = res.content;
            setPublishState(false);
            setStatus("Changes discarded");
            renderPanel();
            refreshPreview();
        } catch (err) { A.toast(err.message, "error"); }
    });

    document.addEventListener("keydown", function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s") { e.preventDefault(); flush(); }
    });

    window.addEventListener("beforeunload", function (e) {
        if (saveTimer || saving) { e.preventDefault(); e.returnValue = ""; }
    });

    // ======================================================================
    // Preview (two iframes: the new version loads behind, then swaps in — no flicker)
    // ======================================================================

    var front = 0;

    function previewSrc() {
        return config.previewUrl + (config.previewUrl.indexOf("?") >= 0 ? "&" : "?") + "_=" + Date.now();
    }

    function refreshPreview() {
        var cur = els.frames[front], next = els.frames[1 - front];
        var y = 0;
        try { y = cur.contentWindow.scrollY; } catch (e) { /* not loaded yet */ }
        next.onload = function () {
            try { next.contentWindow.scrollTo({ top: y, behavior: "instant" }); } catch (e) { /* ignore */ }
            next.classList.add("is-front"); next.removeAttribute("aria-hidden"); next.tabIndex = 0;
            cur.classList.remove("is-front"); cur.setAttribute("aria-hidden", "true"); cur.tabIndex = -1;
            front = 1 - front;
        };
        next.src = previewSrc();
    }

    function scrollPreviewTo(section) {
        // Scroll the preview's own window (scrollIntoView would also scroll the admin page).
        try {
            var win = els.frames[front].contentWindow;
            var el = win.document.querySelector('[data-section="' + section + '"]');
            if (el) win.scrollTo({ top: el.getBoundingClientRect().top + win.scrollY, behavior: "smooth" });
        } catch (e) { /* ignore */ }
    }

    function fit() {
        var w = DEVICES[device];
        var availW = els.stage.clientWidth - 32;
        var availH = els.stage.clientHeight - 32;
        var scale = Math.min(1, availW / w);
        els.device.style.width = Math.round(w * scale) + "px";
        els.device.style.height = availH + "px";
        els.frames.forEach(function (f) {
            f.style.width = w + "px";
            f.style.height = Math.round(availH / scale) + "px";
            f.style.transform = "scale(" + scale + ")";
        });
    }

    root.querySelectorAll("[data-device]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            device = btn.dataset.device;
            root.querySelectorAll("[data-device]").forEach(function (b) { b.classList.toggle("is-active", b === btn); });
            fit();
        });
    });

    new ResizeObserver(fit).observe(els.stage);

    // Clicking a section inside the preview opens its panel (see wwwroot/js/preview.js).
    window.addEventListener("message", function (e) {
        if (e.origin !== location.origin || !e.data || e.data.type !== "chot:edit") return;
        var panel = SECTION_PANEL[e.data.section];
        if (panel && state) openPanel(panel, false);
    });

    // ======================================================================
    // Rail & panels
    // ======================================================================

    function renderRail() {
        els.rail.replaceChildren.apply(els.rail, PANELS.map(function (p) {
            return h("button", {
                type: "button",
                class: "rail-btn" + (p.id === activePanel ? " is-active" : ""),
                "aria-current": p.id === activePanel ? "true" : null,
                onclick: function () { openPanel(p.id, true); },
            }, icon(p.icon), p.rail);
        }));
    }

    function openPanel(id, scrollPreview) {
        activePanel = id;
        renderRail();
        renderPanel();
        if (scrollPreview && PANEL_SECTION[id]) scrollPreviewTo(PANEL_SECTION[id]);
    }

    function renderPanel() {
        var p = PANELS.filter(function (x) { return x.id === activePanel; })[0];
        els.title.textContent = p.title;
        els.hint.textContent = p.hint || "";
        var content = p.render ? p.render() : p.fields.map(function (f) { return renderField(f, ""); });
        els.body.replaceChildren.apply(els.body, [].concat(content));
        els.body.scrollTop = 0;
    }

    // ======================================================================
    // Generic fields
    // ======================================================================

    function hintEl(text) { return text ? h("span", { class: "field__hint", text: text }) : null; }

    function switchControl(label, checked, onChange, ariaLabel) {
        var input = h("input", { type: "checkbox", checked: !!checked, "aria-label": ariaLabel || null });
        input.addEventListener("change", function () { onChange(input.checked); });
        return h("label", { class: "switch" }, input, h("span", { class: "switch__track" }), label ? h("span", { text: label }) : null);
    }

    function renderField(f, base) {
        var path = base ? base + "." + f.key : f.key;
        var input, wrap;

        switch (f.type) {
            case "group":
                return h("div", { class: "group" },
                    h("h3", { class: "group__title", text: f.title }),
                    f.fields.map(function (sub) { return renderField(sub, base); }));

            case "list":
                return renderList(f, path);

            case "media":
                return mediaField(f, path);

            case "checkbox":
                return switchControl(f.label, getPath(state, path), function (v) { setPath(state, path, v); changed(); });

            case "select":
                input = h("select", null, f.options.map(function (o) {
                    return h("option", { value: o.value, selected: o.value === getPath(state, path), text: o.label });
                }));
                input.addEventListener("change", function () { setPath(state, path, input.value); changed(); });
                return h("label", { class: "field" }, h("span", { text: f.label }), input, hintEl(f.hint));

            case "textarea":
                input = h("textarea", { rows: 4, placeholder: f.placeholder || null });
                input.value = getPath(state, path) || "";
                input.addEventListener("input", function () { setPath(state, path, input.value); changed(); });
                return h("label", { class: "field" }, h("span", { text: f.label }), input, hintEl(f.hint));

            case "paragraphs":
                input = h("textarea", { rows: 7 });
                input.value = (getPath(state, path) || []).join("\n\n");
                input.addEventListener("input", function () {
                    setPath(state, path, input.value.split(/\n\s*\n/).map(function (s) { return s.trim(); }).filter(Boolean));
                    changed();
                });
                return h("label", { class: "field" }, h("span", { text: f.label }), input, hintEl(f.hint));

            case "tags":
                input = h("input", { type: "text", value: (getPath(state, path) || []).join(", ") });
                input.addEventListener("input", function () {
                    setPath(state, path, input.value.split(",").map(function (s) { return s.trim(); }).filter(Boolean));
                    changed();
                });
                return h("label", { class: "field" }, h("span", { text: f.label }), input, hintEl(f.hint));

            default: // text, url
                var isUrl = f.type === "url";
                input = h("input", {
                    type: "text",
                    value: getPath(state, path) || "",
                    placeholder: f.placeholder || (isUrl ? "https://…" : null),
                    inputmode: isUrl ? "url" : null,
                    spellcheck: isUrl ? "false" : null,
                });
                var bad = isUrl ? h("span", { class: "field__error", text: "Use a full link starting with https://, mailto: or tel:", hidden: true }) : null;
                wrap = h("label", { class: "field" }, h("span", { text: f.label }), input, bad, hintEl(f.hint));
                var check = function () {
                    if (!isUrl) return;
                    var ok = validLink(input.value);
                    wrap.classList.toggle("is-invalid", !ok);
                    bad.hidden = ok;
                };
                input.addEventListener("input", function () { setPath(state, path, input.value); check(); changed(); });
                check();
                return wrap;
        }
    }

    function mediaField(f, path) {
        var preview = h("div", { class: "media-field__preview" });
        var removeBtn = h("button", { type: "button", class: "abtn abtn--sm abtn--ghost abtn--danger", text: "Remove", onclick: function () { set(""); } });
        var chooseBtn = h("button", {
            type: "button", class: "abtn abtn--sm abtn--outline",
            onclick: function () { A.openMediaPicker({ kind: f.kind, onSelect: set }); },
        }, icon("a-upload"), f.kind === "video" ? "Choose video" : "Choose photo");

        function paint() {
            var url = getPath(state, path) || "";
            preview.replaceChildren(url ? thumbEl(url, f.kind) : icon(f.kind === "video" ? "a-video" : "a-image"));
            removeBtn.hidden = !url;
        }

        function set(url) {
            setPath(state, path, url);
            paint();
            changed();
            preview.dispatchEvent(new Event("input", { bubbles: true })); // refreshes the list item's thumbnail
        }

        paint();
        return h("div", { class: "field" },
            h("span", { class: "field__label", text: f.label }),
            h("div", { class: "media-field" }, preview, h("div", { class: "media-field__buttons" }, chooseBtn, removeBtn)),
            hintEl(f.hint));
    }

    function renderList(f, path) {
        var wrap = h("div", { class: "list" });
        var openItems = new WeakSet();

        function items() {
            var arr = getPath(state, path);
            if (!Array.isArray(arr)) { arr = []; setPath(state, path, arr); }
            return arr;
        }

        function tool(iconId, label, onClick, disabled) {
            return h("button", {
                type: "button", class: "abtn abtn--icon abtn--ghost", title: label, "aria-label": label, disabled: !!disabled,
                onclick: function (e) { e.preventDefault(); e.stopPropagation(); onClick(); },
            }, icon(iconId));
        }

        function move(i, d) {
            var arr = items(), j = i + d;
            if (j < 0 || j >= arr.length) return;
            var t = arr[i]; arr[i] = arr[j]; arr[j] = t;
            paint(); changed();
        }

        function row(item, i, arr) {
            var itemPath = path + "." + i;
            var title = h("span", { class: "list-item__title" });
            var thumb = h("span", { class: "list-item__thumb" });
            var max = f.max || 50;

            function refresh() {
                title.textContent = f.itemTitle(item, i) || "Untitled";
                var m = f.thumb ? f.thumb(item) : "";
                thumb.replaceChildren(m ? thumbEl(m) : icon(f.icon || "a-image"));
            }
            refresh();

            var details = h("details", { class: "list-item", open: openItems.has(item) },
                h("summary", null, thumb, title,
                    h("span", { class: "list-item__tools" },
                        tool("a-up", "Move up", function () { move(i, -1); }, i === 0),
                        tool("a-down", "Move down", function () { move(i, 1); }, i === arr.length - 1),
                        tool("a-copy", "Duplicate", function () {
                            var copy = JSON.parse(JSON.stringify(item));
                            arr.splice(i + 1, 0, copy); openItems.add(copy); paint(); changed();
                        }, arr.length >= max),
                        tool("a-trash", "Delete", function () {
                            if (!confirm("Delete “" + (f.itemTitle(item, i) || "this item") + "”?")) return;
                            arr.splice(i, 1); paint(); changed();
                        }))),
                h("div", { class: "list-item__body" }, f.fields.map(function (sub) { return renderField(sub, itemPath); })));

            details.addEventListener("toggle", function () { if (details.open) openItems.add(item); else openItems.delete(item); });
            details.addEventListener("input", refresh);
            details.addEventListener("change", refresh);
            return details;
        }

        function paint() {
            var arr = items();
            var max = f.max || 50;
            var head = h("div", { class: "list__head" },
                h("span", { class: "list__title" }, f.label + " ", h("span", { class: "list__count", text: "(" + arr.length + (f.max ? " of " + f.max : "") + ")" })));
            var rows = arr.map(function (item, i) { return row(item, i, arr); });
            var add = h("button", {
                type: "button", class: "add-btn", disabled: arr.length >= max,
                onclick: function () { var it = f.newItem(); arr.push(it); openItems.add(it); paint(); changed(); },
            }, icon("a-plus"), "Add " + (f.singular || "item"));
            wrap.replaceChildren.apply(wrap, [head].concat(rows, [add]));
        }

        paint();
        return wrap;
    }

    // ======================================================================
    // Layout panel
    // ======================================================================

    function renderLayout() {
        var container = h("div");
        var dragFrom = -1;

        function clearMarks() {
            container.querySelectorAll(".drop-before, .drop-after").forEach(function (el) { el.classList.remove("drop-before", "drop-after"); });
        }

        function moveSection(from, to) {
            var secs = state.sections;
            if (from === to || to < 0 || to >= secs.length + 1) return;
            var m = secs.splice(from, 1)[0];
            secs.splice(from < to ? to - 1 : to, 0, m);
            paint(); changed();
        }

        function row(s, i, secs) {
            var r;
            var handle = h("span", { class: "section-row__handle", title: "Drag to reorder" }, icon("a-drag"));
            var up = h("button", { type: "button", class: "abtn abtn--icon abtn--ghost", title: "Move up", "aria-label": "Move up", disabled: i === 0, onclick: function () { moveSection(i, i - 1); } }, icon("a-up"));
            var down = h("button", { type: "button", class: "abtn abtn--icon abtn--ghost", title: "Move down", "aria-label": "Move down", disabled: i === secs.length - 1, onclick: function () { moveSection(i, i + 2); } }, icon("a-down"));
            var visible = switchControl("", s.visible, function (v) { s.visible = v; r.classList.toggle("is-hidden", !v); changed(); }, "Show " + (SECTION_NAMES[s.id] || s.id));
            var navLabel = h("input", { type: "text", value: s.navLabel || "", placeholder: "Menu label", "aria-label": "Menu label" });
            navLabel.addEventListener("input", function () { s.navLabel = navLabel.value; changed(); });
            var inNav = switchControl("In menu", s.showInNav, function (v) { s.showInNav = v; changed(); });

            r = h("div", { class: "section-row" + (s.visible ? "" : " is-hidden") },
                handle,
                h("span", { class: "section-row__name", text: SECTION_NAMES[s.id] || s.id }),
                h("span", { class: "section-row__tools" }, up, down, visible),
                h("div", { class: "section-row__nav" }, navLabel, inNav));

            // Drag only from the handle, so the text box stays easy to use.
            handle.addEventListener("mousedown", function () { r.draggable = true; });
            r.addEventListener("dragstart", function (e) {
                dragFrom = i; r.classList.add("is-dragging");
                e.dataTransfer.effectAllowed = "move";
                e.dataTransfer.setData("text/plain", String(i));
            });
            r.addEventListener("dragend", function () { r.draggable = false; r.classList.remove("is-dragging"); clearMarks(); dragFrom = -1; });
            r.addEventListener("dragover", function (e) {
                if (dragFrom < 0) return;
                e.preventDefault();
                var rect = r.getBoundingClientRect();
                var after = e.clientY > rect.top + rect.height / 2;
                clearMarks();
                r.classList.add(after ? "drop-after" : "drop-before");
                r.dataset.after = after ? "1" : "";
            });
            r.addEventListener("drop", function (e) {
                e.preventDefault();
                var from = dragFrom;
                clearMarks();
                if (from >= 0) moveSection(from, i + (r.dataset.after === "1" ? 1 : 0));
            });
            return r;
        }

        function paint() {
            var secs = state.sections;
            container.replaceChildren.apply(container, secs.map(function (s, i) { return row(s, i, secs); }));
        }

        paint();

        var reset = h("div", { class: "danger-zone" },
            h("p", { text: "Start over with the original design and text. Your uploads stay in the media library. Nothing goes live until you publish." }),
            h("button", {
                type: "button", class: "abtn abtn--sm abtn--outline abtn--danger",
                onclick: async function () {
                    if (!confirm("Replace your draft with the original design and text?")) return;
                    try {
                        clearTimeout(saveTimer); saveTimer = null;
                        await flush();
                        var res = await A.api("POST", "reset");
                        state = res.content;
                        setPublishState(res.hasUnpublishedChanges);
                        renderPanel(); refreshPreview();
                        A.toast("Draft reset to the original design.", "ok");
                    } catch (err) { A.toast(err.message, "error"); }
                },
            }, icon("a-undo"), "Restore original design"));

        return [container, reset];
    }

    // ======================================================================
    // Theme panel
    // ======================================================================

    function renderTheme() {
        var t = state.theme;

        function isActive(p) {
            return Object.keys(p.colors).every(function (k) { return (t[k] || "").toUpperCase() === p.colors[k].toUpperCase(); });
        }

        var presets = h("div", { class: "presets" }, PRESETS.map(function (p) {
            return h("button", {
                type: "button", class: "preset" + (isActive(p) ? " is-active" : ""),
                onclick: function () { Object.assign(t, p.colors); renderPanel(); changed(); },
            }, h("span", { class: "preset__swatches" }, [p.colors.primary, p.colors.dark, p.colors.surface].map(function (c) {
                return h("span", { style: "background:" + c });
            })), p.name);
        }));

        var COLORS = [
            ["primary", "Main colour", "Buttons, the big name, the video section and accents."],
            ["dark", "Headline colour", "Dark words like the first line of your name."],
            ["text", "Text colour", null],
            ["surface", "Soft card colour", "Social cards, brand tiles and tags."],
            ["background", "Page background", null],
        ];

        var colorFields = COLORS.map(function (c) {
            var key = c[0];
            var picker = h("input", { type: "color", value: t[key], "aria-label": c[1] });
            var hex = h("input", { type: "text", value: t[key], maxlength: "7", spellcheck: "false", "aria-label": c[1] + " hex code" });
            picker.addEventListener("input", function () { hex.value = picker.value.toUpperCase(); t[key] = hex.value; changed(); });
            hex.addEventListener("input", function () {
                var v = hex.value.trim();
                if (/^#[0-9a-f]{6}$/i.test(v)) { t[key] = v.toUpperCase(); picker.value = v; changed(); }
            });
            return h("div", { class: "field" }, h("span", { class: "field__label", text: c[1] }), h("div", { class: "color-field" }, picker, hex), hintEl(c[2]));
        });

        function fontField(key, label, options, sampleText) {
            var sample = h("div", { class: "font-sample", text: sampleText });
            var paint = function () {
                var o = options.filter(function (x) { return x.name === t[key]; })[0] || options[0];
                sample.style.fontFamily = o.css;
                sample.style.fontWeight = o.weight;
            };
            var sel = h("select", null, options.map(function (o) { return h("option", { value: o.name, selected: o.name === t[key], text: o.name }); }));
            sel.addEventListener("change", function () { t[key] = sel.value; paint(); changed(); });
            paint();
            return h("label", { class: "field" }, h("span", { text: label }), sel, sample);
        }

        return [
            h("div", { class: "group" }, h("h3", { class: "group__title", text: "Presets" }), presets),
            h("div", { class: "group" }, h("h3", { class: "group__title", text: "Colours" }), colorFields),
            h("div", { class: "group" },
                h("h3", { class: "group__title", text: "Fonts" }),
                fontField("displayFont", "Big headlines", config.fonts.display, "CHOT UMANDAL"),
                fontField("scriptFont", "Script accents", config.fonts.script, "content creator"),
                fontField("bodyFont", "Body text", config.fonts.body, "Lifestyle, beauty & everyday content")),
        ];
    }

    // ======================================================================
    // Start
    // ======================================================================

    els.frames[0].src = previewSrc();
    fit();

    A.api("GET", "draft").then(function (res) {
        state = res.content;
        setPublishState(res.hasUnpublishedChanges);
        setStatus(res.hasUnpublishedChanges ? "Draft has unpublished changes" : "All changes are live");
        renderRail();
        renderPanel();
    }).catch(function (err) {
        els.body.replaceChildren(h("p", { class: "muted", text: "Couldn't load your site: " + err.message }));
        setStatus("Couldn't load", true);
    });
})();
