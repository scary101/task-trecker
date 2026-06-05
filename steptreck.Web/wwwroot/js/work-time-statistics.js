const charts = new Map();

export function renderWorkTimeStatistics(options) {
    const canvas = document.getElementById(options?.canvasId || '');
    if (!canvas || typeof Chart === 'undefined') {
        return;
    }

    const labels = Array.isArray(options.labels) ? options.labels : [];
    const values = Array.isArray(options.values) ? options.values : [];
    const sessions = Array.isArray(options.sessions) ? options.sessions : [];

    const palette = readPalette();
    const existing = charts.get(canvas.id);
    if (existing) {
        existing.data.labels = labels;
        existing.data.datasets[0].data = values;
        existing.data.datasets[0].pointRadius = values.map((value) => value > 0 ? 4 : 2);
        existing.data.datasets[0].pointHoverRadius = values.map((value) => value > 0 ? 6 : 4);
        existing.options.plugins.tooltip.callbacks = buildTooltipCallbacks(labels, values, sessions);
        existing.options.scales.x.ticks.color = palette.muted;
        existing.options.scales.y.ticks.color = palette.muted;
        existing.options.scales.x.grid.color = palette.grid;
        existing.options.scales.y.grid.color = palette.grid;
        existing.update();
        return;
    }

    const chart = new Chart(canvas.getContext('2d'), {
        type: 'line',
        data: {
            labels,
            datasets: [
                {
                    label: 'Часы работы',
                    data: values,
                    fill: true,
                    tension: 0.35,
                    borderWidth: 3,
                    borderColor: palette.line,
                    backgroundColor: palette.area,
                    pointBackgroundColor: palette.surface,
                    pointBorderColor: palette.line,
                    pointBorderWidth: 3,
                    pointRadius: values.map((value) => value > 0 ? 4 : 2),
                    pointHoverRadius: values.map((value) => value > 0 ? 6 : 4)
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: {
                duration: 260
            },
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: palette.tooltipBg,
                    titleColor: palette.tooltipTitle,
                    bodyColor: palette.tooltipBody,
                    borderColor: palette.tooltipBorder,
                    borderWidth: 1,
                    padding: 12,
                    displayColors: false,
                    callbacks: buildTooltipCallbacks(labels, values, sessions)
                }
            },
            scales: {
                x: {
                    ticks: {
                        color: palette.muted,
                        maxTicksLimit: 8
                    },
                    grid: {
                        color: palette.grid,
                        drawBorder: false
                    },
                    border: {
                        display: false
                    }
                },
                y: {
                    beginAtZero: true,
                    ticks: {
                        color: palette.muted,
                        callback: (value) => `${value} ч`
                    },
                    grid: {
                        color: palette.grid,
                        drawBorder: false
                    },
                    border: {
                        display: false
                    }
                }
            }
        }
    });

    charts.set(canvas.id, chart);
}

export function disposeWorkTimeStatistics(canvasId) {
    const chart = charts.get(canvasId);
    if (!chart) {
        return;
    }

    chart.destroy();
    charts.delete(canvasId);
}

function buildTooltipCallbacks(labels, values, sessions) {
    return {
        title: (items) => {
            if (!items || !items.length) {
                return '';
            }
            return labels[items[0].dataIndex] || '';
        },
        label: (ctx) => {
            const index = ctx.dataIndex;
            const hours = Number(values[index] || 0).toLocaleString('ru-RU', { maximumFractionDigits: 2 });
            const sessionCount = sessions[index] || 0;
            return `Время: ${hours} ч | Сессии: ${sessionCount}`;
        }
    };
}

function readPalette() {
    const style = getComputedStyle(document.documentElement);
    const line = readCssVar(style, '--primary', '#60a5fa');
    const surface = readCssVar(style, '--surface', '#ffffff');
    const muted = rgbaFromCssVar(style, '--text-muted-rgb', 'rgba(148, 163, 184, 0.86)');
    const grid = rgbaFromCssVar(style, '--border-strong-rgb', 'rgba(148, 163, 184, 0.16)', 0.18);
    const tooltipBg = rgbaFromCssVar(style, '--bg-main-rgb', 'rgba(15, 23, 42, 0.96)', 0.96);
    const tooltipTitle = readCssVar(style, '--text-primary', '#f8fafc');
    const tooltipBody = readCssVar(style, '--text-secondary', '#cbd5e1');
    const tooltipBorder = rgbaFromCssVar(style, '--border-rgb', 'rgba(148, 163, 184, 0.26)', 0.32);
    const primaryRgb = style.getPropertyValue('--primary-rgb').trim() || '96, 165, 250';

    return {
        line,
        surface,
        muted,
        grid,
        tooltipBg,
        tooltipTitle,
        tooltipBody,
        tooltipBorder,
        area: `rgba(${primaryRgb}, 0.18)`
    };
}

function readCssVar(style, name, fallback) {
    return style.getPropertyValue(name).trim() || fallback;
}

function rgbaFromCssVar(style, name, fallback, alphaOverride) {
    const raw = style.getPropertyValue(name).trim();
    if (!raw) {
        return fallback;
    }

    if (typeof alphaOverride === 'number') {
        return `rgba(${raw}, ${alphaOverride})`;
    }

    return `rgba(${raw}, 1)`;
}
