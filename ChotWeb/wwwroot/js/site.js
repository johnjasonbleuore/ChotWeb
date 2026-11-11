(function () {
    "use strict";

    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // ---- Mobile menu ----
    var menuBtn = document.querySelector("[data-menu-toggle]");
    var menu = document.getElementById("mobile-menu");

    if (menuBtn && menu) {
        var setOpen = function (open) {
            menu.hidden = !open;
            menuBtn.setAttribute("aria-expanded", String(open));
            menuBtn.setAttribute("aria-label", open ? "Close menu" : "Open menu");
        };

        menuBtn.addEventListener("click", function () { setOpen(menu.hidden); });
        menu.addEventListener("click", function (e) { if (e.target.closest("a")) setOpen(false); });
        document.addEventListener("keydown", function (e) { if (e.key === "Escape") setOpen(false); });
        window.matchMedia("(min-width: 761px)").addEventListener("change", function (e) { if (e.matches) setOpen(false); });
    }

    // ---- Video carousel ----
    document.querySelectorAll("[data-carousel]").forEach(function (root) {
        var track = root.querySelector("[data-carousel-track]");
        var prev = root.querySelector("[data-carousel-prev]");
        var next = root.querySelector("[data-carousel-next]");
        if (!track || !prev || !next) return;

        var step = function () {
            var card = track.firstElementChild;
            var gap = parseFloat(getComputedStyle(track).columnGap) || 0;
            return card ? card.getBoundingClientRect().width + gap : track.clientWidth;
        };

        var update = function () {
            var max = track.scrollWidth - track.clientWidth;
            prev.disabled = track.scrollLeft <= 2;
            next.disabled = track.scrollLeft >= max - 2;
        };

        var go = function (dir) {
            track.scrollBy({ left: dir * step(), behavior: reduceMotion ? "auto" : "smooth" });
        };

        prev.addEventListener("click", function () { go(-1); });
        next.addEventListener("click", function () { go(1); });
        track.addEventListener("scroll", update, { passive: true });
        window.addEventListener("resize", update);
        update();
    });

    // ---- Pop-up player for uploaded videos ----
    var modal = document.querySelector("[data-video-modal]");
    if (modal && typeof modal.showModal === "function") {
        var player = modal.querySelector("video");

        document.addEventListener("click", function (e) {
            var trigger = e.target.closest("[data-video-src]");
            if (!trigger) return;
            e.preventDefault();
            player.src = trigger.getAttribute("data-video-src");
            modal.showModal();
            var playing = player.play();
            if (playing && playing.catch) playing.catch(function () { /* autoplay blocked: user presses play */ });
        });

        modal.addEventListener("click", function (e) { if (e.target === modal) modal.close(); });
        modal.addEventListener("close", function () {
            player.pause();
            player.removeAttribute("src");
            player.load();
        });
    }

    // ---- Highlight the nav link of the section on screen ----
    var links = Array.prototype.slice.call(document.querySelectorAll("[data-nav-link]"));
    var sections = links
        .map(function (a) { return document.getElementById(a.getAttribute("data-nav-link")); })
        .filter(Boolean);

    var setActive = function (id) {
        links.forEach(function (a) { a.classList.toggle("is-active", a.getAttribute("data-nav-link") === id); });
    };

    if (sections.length && "IntersectionObserver" in window) {
        setActive(sections[0].id);
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) { if (entry.isIntersecting) setActive(entry.target.id); });
        }, { rootMargin: "-45% 0px -50% 0px" });
        sections.forEach(function (s) { observer.observe(s); });
    }
})();
