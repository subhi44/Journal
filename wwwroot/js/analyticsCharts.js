console.log("analyticsCharts.js loaded");

window.logbookCharts = {
    moodChart: null,
    tagChart: null,
    wordChart: null,

    // --- read CSS variables so charts match your theme tokens ---
    _css(name, fallback) {
        const v = getComputedStyle(document.body).getPropertyValue(name).trim();
        return v || fallback;
    },

    _theme() {
        // Use your existing CSS tokens
        const text = this._css("--text", "#111827");
        const muted = this._css("--text-muted", "#6b7280");
        const border = this._css("--border", "rgba(0,0,0,0.12)");
        const surface = this._css("--surface", "#ffffff");
        const primary = this._css("--primary", "#ec4899");
        const primary2 = this._css("--primary-2", "#db2777");

        const isDark = document.body.classList.contains("dark-mode");

        // Gridline color: subtle in both modes
        const grid = isDark ? "rgba(255,255,255,0.10)" : "rgba(17,24,39,0.10)";

        return { text, muted, border, surface, primary, primary2, grid, isDark };
    },

    // Build a nice palette (no need to hardcode per mood/tag)
    _palette(n) {
        // A soft modern palette (works with your pink/purple theme)
        const base = [
            "#ec4899", "#a855f7", "#22c55e", "#f59e0b", "#06b6d4",
            "#ef4444", "#14b8a6", "#6366f1", "#84cc16", "#f97316",
            "#e879f9", "#0ea5e9"
        ];
        const out = [];
        for (let i = 0; i < n; i++) out.push(base[i % base.length]);
        return out;
    },

    _commonOptions() {
        const t = this._theme();

        return {
            responsive: true,
            maintainAspectRatio: false, // IMPORTANT: lets you control height via CSS (.chart-canvas)
            animation: { duration: 450 },
            layout: { padding: { top: 6, right: 8, bottom: 6, left: 8 } },
            plugins: {
                legend: {
                    labels: {
                        color: t.muted,
                        boxWidth: 10,
                        boxHeight: 10,
                        usePointStyle: true,
                        pointStyle: "circle",
                        padding: 14,
                        font: { weight: "700" }
                    }
                },
                tooltip: {
                    backgroundColor: t.surface,
                    titleColor: t.text,
                    bodyColor: t.muted,
                    borderColor: t.border,
                    borderWidth: 1,
                    padding: 12,
                    displayColors: true
                }
            }
        };
    },

    _axisOptions() {
        const t = this._theme();
        return {
            ticks: {
                color: t.muted,
                font: { weight: "700" }
            },
            grid: {
                color: t.grid,
                drawBorder: false
            }
        };
    },

    renderOrUpdate: function (payload) {
        const t = this._theme();
        const common = this._commonOptions();
        const axis = this._axisOptions();

        // ---- Mood chart (Doughnut instead of Pie) ----
        const moodCtx = document.getElementById("moodChart");
        if (moodCtx) {
            if (this.moodChart) this.moodChart.destroy();

            const colors = this._palette(payload.moodLabels?.length || 0);

            this.moodChart = new Chart(moodCtx, {
                type: "doughnut",
                data: {
                    labels: payload.moodLabels,
                    datasets: [{
                        data: payload.moodValues,
                        backgroundColor: colors,
                        borderColor: t.surface,
                        borderWidth: 2,
                        hoverOffset: 6
                    }]
                },
                options: {
                    ...common,
                    cutout: "62%",
                    plugins: {
                        ...common.plugins,
                        legend: { ...common.plugins.legend, position: "bottom" }
                    }
                }
            });
        }

        // ---- Tags chart (Bar) - nicer bars + grid + rounded corners ----
        const tagCtx = document.getElementById("tagChart");
        if (tagCtx) {
            if (this.tagChart) this.tagChart.destroy();

            this.tagChart = new Chart(tagCtx, {
                type: "bar",
                data: {
                    labels: payload.tagLabels,
                    datasets: [{
                        data: payload.tagValues,
                        backgroundColor: t.primary,
                        borderColor: t.primary2,
                        borderWidth: 1,
                        borderRadius: 10,
                        maxBarThickness: 42
                    }]
                },
                options: {
                    ...common,
                    plugins: { ...common.plugins, legend: { display: false } },
                    scales: {
                        x: { ...axis },
                        y: { ...axis, beginAtZero: true }
                    }
                }
            });
        }

        // ---- Word trend (Line) - gradient fill + smoother line ----
        const wordCtx = document.getElementById("wordChart");
        if (wordCtx) {
            if (this.wordChart) this.wordChart.destroy();

            // gradient fill
            const ctx2d = wordCtx.getContext("2d");
            const grad = ctx2d.createLinearGradient(0, 0, 0, 260);
            grad.addColorStop(0, t.isDark ? "rgba(168,85,247,0.35)" : "rgba(236,72,153,0.25)");
            grad.addColorStop(1, "rgba(0,0,0,0)");

            this.wordChart = new Chart(wordCtx, {
                type: "line",
                data: {
                    labels: payload.wordLabels,
                    datasets: [{
                        data: payload.wordValues,
                        borderColor: t.primary,
                        backgroundColor: grad,
                        fill: true,
                        tension: 0.35,
                        pointRadius: 3,
                        pointHoverRadius: 6,
                        pointBackgroundColor: t.surface,
                        pointBorderColor: t.primary,
                        pointBorderWidth: 2,
                        borderWidth: 3
                    }]
                },
                options: {
                    ...common,
                    plugins: { ...common.plugins, legend: { display: false } },
                    scales: {
                        x: { ...axis },
                        y: { ...axis, beginAtZero: true }
                    }
                }
            });
        }
    }
};
