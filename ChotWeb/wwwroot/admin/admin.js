// Shared admin helpers + media library page + media picker dialog.
window.ChotAdmin = (function () {
    "use strict";

    var token = (document.querySelector('meta[name="csrf-token"]') || {}).content || "";
    var base = (document.querySelector('meta[name="app-base"]') || {}).content || "/";
    var LIMITS = { image: 15 * 1024 * 1024, video: 300 * 1024 * 1024 };
    var ACCEPT = {
        image: "image/jpeg,image/png,image/webp,image/gif,image/avif,.jpg,.jpeg,.png,.webp,.gif,.avif",
        video: "video/mp4,video/webm,video/quicktime,.mp4,.m4v,.mov,.webm",
    };
    ACCEPT.all = ACCEPT.image + "," + ACCEPT.video;

    // ---------- small DOM helper ----------
    function h(tag, attrs) {
        var el = document.createElement(tag);
        var children = Array.prototype.slice.call(arguments, 2);
        if (attrs) {
            Object.keys(attrs).forEach(function (k) {
                var v = attrs[k];
                if (v === null || v === undefined || v === false) return;
                if (k === "class") el.className = v;
                else if (k === "text") el.textContent = v;
                else if (k.slice(0, 2) === "on") el.addEventListener(k.slice(2), v);
                else if (k === "dataset") Object.assign(el.dataset, v);
                else if (v === true) el.setAttribute(k, "");
                else el.setAttribute(k, v);
            });
        }
        children.flat(Infinity).forEach(function (c) {
            if (c === null || c === undefined || c === false) return;
            el.appendChild(typeof c === "string" || typeof c === "number" ? document.createTextNode(String(c)) : c);
        });
        return el;
    }

    function icon(id, cls) {
        var svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("class", cls || "ai");
        svg.setAttribute("aria-hidden", "true");
        var use = document.createElementNS("http://www.w3.org/2000/svg", "use");
        use.setAttribute("href", "#" + id);
        svg.appendChild(use);
        return svg;
    }

    function resolveUrl(url) {
        if (!url) return "";
        return url.indexOf("~/") === 0 ? base + url.slice(2) : url;
    }

    function kindOf(url) {
        return /\.(mp4|m4v|mov|webm)(\?|#|$)/i.test(url || "") ? "video" : "image";
    }

    function formatSize(bytes) {
        if (bytes >= 1024 * 1024) return (bytes / 1024 / 1024).toFixed(1) + " MB";
        return Math.max(1, Math.round(bytes / 1024)) + " KB";
    }

    function toast(message, type) {
        var box = document.querySelector("[data-toasts]");
        if (!box) return;
        var t = h("div", { class: "toast" + (type ? " toast--" + type : ""), role: "status", text: message });
        box.appendChild(t);
        setTimeout(function () { t.remove(); }, type === "error" ? 6000 : 3000);
    }

    // ---------- API ----------
    function toLogin() {
        location.href = base + "admin/login?returnUrl=" + encodeURIComponent(location.pathname);
    }

    async function api(method, path, body) {
        var headers = { RequestVerificationToken: token };
        if (body !== undefined) headers["Content-Type"] = "application/json";
        var res = await fetch(base + "admin/api/" + path, {
            method: method,
            headers: headers,
            body: body !== undefined ? JSON.stringify(body) : undefined,
            credentials: "same-origin",
        });
        if (res.status === 401) { toLogin(); throw new Error("You were signed out."); }
        if (!res.ok) {
            var msg = "Something went wrong (" + res.status + ").";
            try { var j = await res.json(); msg = j.error || j.title || msg; } catch (e) { /* not JSON */ }
            throw new Error(msg);
        }
        return res.status === 204 ? null : res.json();
    }

    function upload(file, onProgress) {
        return new Promise(function (resolve, reject) {
            var kind = /^video\//.test(file.type) || /\.(mp4|m4v|mov|webm)$/i.test(file.name) ? "video" : "image";
            if (file.size > LIMITS[kind]) {
                reject(new Error(file.name + " is too large (limit " + formatSize(LIMITS[kind]) + ")."));
                return;
            }
            var form = new FormData();
            form.append("file", file);
            var xhr = new XMLHttpRequest();
            xhr.open("POST", base + "admin/api/media");
            xhr.setRequestHeader("RequestVerificationToken", token);
            xhr.upload.onprogress = function (e) { if (e.lengthComputable && onProgress) onProgress(e.loaded / e.total); };
            xhr.onload = function () {
                var data = null;
                try { data = JSON.parse(xhr.responseText); } catch (e) { /* ignore */ }
                if (xhr.status === 401) { toLogin(); reject(new Error("You were signed out.")); }
                else if (xhr.status >= 200 && xhr.status < 300) resolve(data);
                else if (xhr.status === 413) reject(new Error(file.name + " is too large for the server."));
                else reject(new Error((data && data.error) || "Upload failed (" + xhr.status + ")."));
            };
            xhr.onerror = function () { reject(new Error("Upload failed — check your connection.")); };
            xhr.send(form);
        });
    }

    // ---------- media grid (used by the library page and the picker) ----------
    function mediaThumb(item) {
        var src = resolveUrl(item.url);
        return item.kind === "video"
            ? h("video", { src: src + "#t=0.5", muted: true, preload: "metadata", playsinline: true })
            : h("img", { src: src, alt: "", loading: "lazy" });
    }

    function uploadingCard(file) {
        var bar = h("span");
        var card = h("div", { class: "media-card is-uploading" },
            h("div", { class: "media-card__thumb" }, h("div", { class: "progress" }, bar)),
            h("div", { class: "media-card__info" }, h("div", { class: "media-card__meta", text: file.name })));
        card.setProgress = function (p) { bar.style.width = Math.round(p * 100) + "%"; };
        return card;
    }

    function dropzone(kind, onFiles) {
        var input = h("input", { type: "file", accept: ACCEPT[kind || "all"], multiple: true, hidden: true });
        var zone = h("div", { class: "dropzone", role: "button", tabindex: "0" },
            icon("a-upload"),
            h("strong", { text: "Drop " + (kind === "video" ? "videos" : kind === "image" ? "photos" : "photos or videos") + " here" }),
            h("span", { text: "or click to choose from your computer" }),
            input);
        var open = function () { input.click(); };
        zone.addEventListener("click", function (e) { if (e.target !== input) open(); });
        zone.addEventListener("keydown", function (e) { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); open(); } });
        input.addEventListener("change", function () { onFiles(Array.from(input.files)); input.value = ""; });
        ["dragenter", "dragover"].forEach(function (ev) {
            zone.addEventListener(ev, function (e) { e.preventDefault(); zone.classList.add("is-over"); });
        });
        ["dragleave", "drop"].forEach(function (ev) {
            zone.addEventListener(ev, function (e) { e.preventDefault(); zone.classList.remove("is-over"); });
        });
        zone.addEventListener("drop", function (e) { onFiles(Array.from(e.dataTransfer.files)); });
        zone.open = open;
        return zone;
    }

    async function uploadAll(files, grid, onDone) {
        for (var i = 0; i < files.length; i++) {
            var file = files[i];
            var card = uploadingCard(file);
            grid.prepend(card);
            try {
                var item = await upload(file, card.setProgress);
                card.remove();
                onDone(item);
                toast("Uploaded " + file.name, "ok");
            } catch (err) {
                card.remove();
                toast(err.message, "error");
            }
        }
    }

    // ---------- Media library page (/admin/media) ----------
    function mediaLibrary(root) {
        var filter = "";
        var items = [];
        var grid = h("div", { class: "media-grid" });
        var empty = h("p", { class: "empty", text: "No uploads yet. Drop photos or videos above to get started." });

        var tabs = h("div", { class: "segmented", role: "group", "aria-label": "Filter" },
            [["", "All"], ["image", "Photos"], ["video", "Videos"]].map(function (t) {
                return h("button", {
                    type: "button", class: t[0] === "" ? "is-active" : null, text: t[1],
                    onclick: function (e) {
                        filter = t[0];
                        tabs.querySelectorAll("button").forEach(function (b) { b.classList.toggle("is-active", b === e.currentTarget); });
                        render();
                    },
                });
            }));

        var zone = dropzone(null, function (files) {
            uploadAll(files, grid, function (item) { items.unshift(item); render(); });
        });

        function card(item) {
            return h("div", { class: "media-card" },
                h("a", { class: "media-card__thumb", href: resolveUrl(item.url), target: "_blank", rel: "noopener", title: "Open" },
                    mediaThumb(item),
                    h("span", { class: "media-card__kind" }, icon(item.kind === "video" ? "a-video" : "a-image"))),
                h("div", { class: "media-card__info" },
                    h("div", { class: "media-card__meta" },
                        h("div", { text: formatSize(item.size) + " · " + new Date(item.uploadedAt).toLocaleDateString() }),
                        item.inUse ? h("span", { class: "badge badge--use", text: "Used on site" }) : null),
                    h("button", {
                        class: "abtn abtn--icon abtn--ghost", type: "button", title: "Copy link",
                        onclick: function () {
                            var full = new URL(resolveUrl(item.url), location.href).href;
                            navigator.clipboard.writeText(full).then(function () { toast("Link copied", "ok"); });
                        },
                    }, icon("a-copy")),
                    h("button", {
                        class: "abtn abtn--icon abtn--danger", type: "button", title: "Delete",
                        onclick: async function () {
                            var msg = item.inUse
                                ? "This file is used on your site. Deleting it will leave an empty spot. Delete anyway?"
                                : "Delete this file?";
                            if (!confirm(msg)) return;
                            try {
                                await api("DELETE", "media/" + encodeURIComponent(item.name));
                                items = items.filter(function (i) { return i.name !== item.name; });
                                render();
                                toast("Deleted", "ok");
                            } catch (err) { toast(err.message, "error"); }
                        },
                    }, icon("a-trash"))));
        }

        function render() {
            var shown = items.filter(function (i) { return !filter || i.kind === filter; });
            grid.replaceChildren.apply(grid, shown.map(card));
            empty.hidden = shown.length > 0;
        }

        root.append(zone, h("div", { class: "media-toolbar" }, tabs), grid, empty);
        api("GET", "media").then(function (list) { items = list; render(); })
            .catch(function (err) { toast(err.message, "error"); });
    }

    // ---------- Media picker dialog (used by the designer) ----------
    function openMediaPicker(options) {
        var kind = options.kind || "image";
        var grid = h("div", { class: "media-grid" });
        var empty = h("p", { class: "empty", text: "Nothing here yet — upload one above." });
        var dialog;

        function choose(item) {
            dialog.close();
            options.onSelect(item.url);
        }

        function card(item) {
            return h("div", { class: "media-card" },
                h("button", { class: "media-card__thumb", type: "button", title: "Use this", onclick: function () { choose(item); } },
                    mediaThumb(item)),
                h("div", { class: "media-card__info" },
                    h("div", { class: "media-card__meta", text: formatSize(item.size) })));
        }

        var zone = dropzone(kind, function (files) {
            uploadAll(files.slice(0, 1), grid, function (item) {
                if (item.kind !== kind) { toast("Please choose a " + (kind === "video" ? "video" : "photo") + ".", "error"); return; }
                choose(item);
            });
        });

        dialog = h("dialog", { class: "picker", "aria-label": options.title || "Choose media" },
            h("div", { class: "picker__head" },
                h("h2", { text: options.title || (kind === "video" ? "Choose a video" : "Choose a photo") }),
                h("button", { class: "abtn abtn--icon abtn--ghost", type: "button", title: "Close", onclick: function () { dialog.close(); } }, icon("a-x"))),
            h("div", { class: "picker__body" }, zone, grid, empty));

        dialog.addEventListener("close", function () { dialog.remove(); });
        dialog.addEventListener("click", function (e) { if (e.target === dialog) dialog.close(); });
        document.body.appendChild(dialog);
        dialog.showModal();

        api("GET", "media?kind=" + kind).then(function (list) {
            grid.replaceChildren.apply(grid, list.map(card));
            empty.hidden = list.length > 0;
        }).catch(function (err) { toast(err.message, "error"); });
    }

    document.querySelectorAll("[data-media-library]").forEach(mediaLibrary);

    return { api: api, upload: upload, h: h, icon: icon, toast: toast, resolveUrl: resolveUrl, kindOf: kindOf, formatSize: formatSize, openMediaPicker: openMediaPicker };
})();
