// Loaded only when an admin previews the draft inside the designer (/admin).
// Hovering a section outlines it; clicking it tells the designer which panel to open.
(function () {
    "use strict";
    if (window.parent === window) return;

    var labels = { header: "Header", hero: "Hero", videos: "Videos", photos: "Photos", brands: "Brands", contact: "Contact", footer: "Footer" };
    var tag = document.createElement("div");
    tag.className = "preview-tag";
    tag.hidden = true;
    document.body.appendChild(tag);
    var current = null;

    var show = function (section) {
        if (current === section) return;
        if (current) current.classList.remove("is-hovered");
        current = section;
        if (!section) { tag.hidden = true; return; }
        section.classList.add("is-hovered");
        var r = section.getBoundingClientRect();
        tag.textContent = "✎ Edit " + (labels[section.getAttribute("data-section")] || "section");
        tag.style.top = (Math.max(r.top, 0) + window.scrollY + 10) + "px";
        tag.style.left = (r.left + window.scrollX + 10) + "px";
        tag.hidden = false;
    };

    document.addEventListener("mouseover", function (e) { show(e.target.closest("[data-section]")); });
    document.addEventListener("mouseleave", function () { show(null); });
    window.addEventListener("scroll", function () { show(null); }, { passive: true });

    document.addEventListener("click", function (e) {
        var link = e.target.closest("a");
        // Allow in-page anchor links (menu); block links that would leave the preview.
        var inPage = link && link.hash && link.pathname === location.pathname && link.origin === location.origin;
        if (link && !inPage && !link.hasAttribute("data-video-src")) e.preventDefault();

        var section = e.target.closest("[data-section]");
        if (section && !inPage) {
            window.parent.postMessage({ type: "chot:edit", section: section.getAttribute("data-section") }, location.origin);
        }
    }, true);
})();
