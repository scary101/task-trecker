const charts = new Map();
let timerId = null;
let apiBase = '';
let minutes = 60;
let updateMs = 15000;
let authToken = '';
let inFlight = false;
let lastPayload = {};

const metricConfigs = {
    rps: {
        id: 'chart-rps',
        unit: 'rps',
        color: '#22d3ee',
        formatKpi: formatCompact,
        formatAxis: formatCompact,
        title: 'Запросы в секунду'
    },
    p95: {
        id: 'chart-p95',
        unit: 'ms',
        color: '#f97316',
        formatKpi: (v) => formatDuration(v),
        formatAxis: (v) => formatDuration(v),
        title: 'P95 задержки'
    },
    '5xx': {
        id: 'chart-5xx',
        unit: 'errors',
        color: '#f472b6',
        formatKpi: formatCompact,
        formatAxis: formatCompact,
        title: 'Ошибки 5xx'
    },
    cpu: {
        id: 'chart-cpu',
        unit: '%',
        color: '#34d399',
        formatKpi: (v) => `${v.toFixed(1)}%`,
        formatAxis: (v) => `${v}%`,
        title: 'CPU'
    },
    ram: {
        id: 'chart-ram',
        unit: 'bytes',
        color: '#60a5fa',
        formatKpi: formatBytes,
        formatAxis: (v) => formatBytes(v, 0),
        title: 'RAM'
    },
    active: {
        id: 'chart-active',
        unit: 'req',
        color: '#a78bfa',
        formatKpi: formatCompact,
        formatAxis: formatCompact,
        title: 'Активные запросы'
    }
};

export function initOpsMetrics(options) {
    apiBase = normalizeBase(options.apiBase || '');
    minutes = clampMinutes(options.minutes || 60);
    updateMs = options.updateMs || 15000;
    authToken = options.token || '';

    ensureCharts();
    bindDownloadButtons();
    updateRangeLabel();
    refreshNow();

    if (timerId) {
        clearInterval(timerId);
    }
    timerId = setInterval(refreshNow, updateMs);
}

export function setRange(value) {
    minutes = clampMinutes(value);
    updateRangeLabel();
    refreshNow();
}

export function refreshNow() {
    if (inFlight) return;
    inFlight = true;
    setStatus('Обновляем данные...');

    fetchDashboard()
        .then(() => setStatus(`Обновлено: ${formatNow()}`))
        .catch((err) => {
            console.error('OPS METRICS ERROR:', err);
            setStatus(`Ошибка обновления: ${err?.message || err}`);
        })
        .finally(() => {
            inFlight = false;
        });
}


export function disposeOpsMetrics() {
    if (timerId) {
        clearInterval(timerId);
        timerId = null;
    }
    charts.forEach((chart) => chart.destroy());
    charts.clear();
    lastPayload = {};
}

function ensureCharts() {
    Object.keys(metricConfigs).forEach((key) => {
        const config = metricConfigs[key];
        if (charts.has(key)) {
            return;
        }
        const canvas = document.getElementById(config.id);
        if (!canvas) {
            return;
        }
        const ctx = canvas.getContext('2d');
        const chart = new Chart(ctx, buildChartConfig(config));
        charts.set(key, chart);
    });
}

function bindDownloadButtons() {
    document.querySelectorAll('[data-ops-download]').forEach((btn) => {
        if (btn.dataset.bound) {
            return;
        }
        btn.dataset.bound = 'true';
        btn.addEventListener('click', () => {
            const key = btn.getAttribute('data-ops-download');
            if (!key) return;
            downloadChart(key);
        });
    });
}

function buildChartConfig(config) {
    return {
        type: 'line',
        data: {
            datasets: []
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            interaction: {
                mode: 'nearest',
                intersect: false
            },
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(15, 23, 42, 0.95)',
                    borderColor: 'rgba(148, 163, 184, 0.3)',
                    borderWidth: 1,
                    titleColor: '#e2e8f0',
                    bodyColor: '#cbd5f5',
                    callbacks: {
                        label: (ctx) => {
                            const value = ctx.parsed.y ?? 0;
                            return `${ctx.dataset.label}: ${config.formatKpi(value)}`;
                        },
                        title: (items) => {
                            if (!items || !items.length) return '';
                            return formatTime(items[0].parsed.x);
                        }
                    }
                }
            },
            scales: {
                x: {
                    type: 'linear',
                    ticks: {
                        color: 'rgba(148, 163, 184, 0.7)',
                        maxTicksLimit: 5,
                        callback: (value) => formatTime(value)
                    },
                    grid: {
                        color: 'rgba(148, 163, 184, 0.08)'
                    }
                },
                y: {
                    ticks: {
                        color: 'rgba(148, 163, 184, 0.7)',
                        callback: (value) => config.formatAxis(value)
                    },
                    grid: {
                        color: 'rgba(148, 163, 184, 0.08)'
                    }
                }
            },
            elements: {
                line: {
                    tension: 0.3,
                    borderWidth: 2
                },
                point: {
                    radius: 0
                }
            }
        }
    };
}

async function fetchDashboard() {
    const url = `${apiBase}ops-metrics/dashboard?minutes=${minutes}`;
    const headers = authToken ? { Authorization: `Bearer ${authToken}` } : {};

    const response = await fetch(url, { credentials: 'include', headers });

    if (!response.ok) {
        const text = await response.text().catch(() => '');
        console.error('FETCH FAILED', {
            url,
            status: response.status,
            statusText: response.statusText,
            body: text
        });
        throw new Error(`${response.status} ${response.statusText}`);
    }

    const payload = await response.json();

    console.log("OPS PAYLOAD:", payload);
    console.log("Chart exists:", typeof Chart);
    console.log("Canvas rps:", document.getElementById("chart-rps"));

    lastPayload = payload || {};

    const seriesMap = {
        rps: lastPayload.rps || [],
        p95: lastPayload.p95 || [],
        '5xx': lastPayload.err5 || lastPayload.err5xx || lastPayload['5xx'] || [],
        cpu: lastPayload.cpu || [],
        ram: lastPayload.ram || [],
        active: lastPayload.act || lastPayload.active || []
    };

    Object.keys(metricConfigs).forEach((key) => {
        const config = metricConfigs[key];
        const series = seriesMap[key] || [];
        updateChart(key, series, config);
        updateKpi(key, series, config);
    });
}


function updateChart(key, series, config) {
    const chart = charts.get(key);
    if (!chart) {
        return;
    }

    const datasets = (series || []).map((item, index) => {
        const data = (item.points || []).map((p) => ({
            x: toMs(p.t),
            y: p.v
        }));
        return {
            label: item.name || `Series ${index + 1}`,
            data,
            borderColor: config.color,
            backgroundColor: hexToRgba(config.color, 0.15),
            fill: true
        };
    });

    chart.data.datasets = datasets;
    chart.update('none');
}

function updateKpi(key, series, config) {
    const latest = getLatest(series);
    const elements = document.querySelectorAll(`[data-ops-kpi="${key}"]`);
    if (!elements.length) {
        return;
    }
    const text = latest == null ? '—' : config.formatKpi(latest);
    elements.forEach((el) => {
        el.textContent = text;
    });

    if (key === '5xx') {
        const tone = document.querySelector('[data-ops-tone]');
        if (tone) {
            tone.classList.remove('tone-ok', 'tone-warn', 'tone-alert', 'tone-neutral');
            if (latest == null) {
                tone.classList.add('tone-neutral');
            } else if (latest >= 10) {
                tone.classList.add('tone-alert');
            } else if (latest >= 1) {
                tone.classList.add('tone-warn');
            } else {
                tone.classList.add('tone-ok');
            }
        }
    }
}

function downloadChart(key) {
    const config = metricConfigs[key];
    if (!config) return;

    const chart = charts.get(key);
    if (!chart) return;

    const canvas = chart.canvas;
    const imgData = canvas.toDataURL('image/png', 1.0);

    const { jsPDF } = window.jspdf || {};
    if (!jsPDF) {
        console.warn('jsPDF not loaded');
        return;
    }

    const doc = new jsPDF({
        orientation: 'landscape',
        unit: 'pt',
        format: 'a4'
    });

    const pageWidth = doc.internal.pageSize.getWidth();
    const pageHeight = doc.internal.pageSize.getHeight();
    const margin = 36;

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(18);
    doc.text(config.title || key, margin, 40);

    const insight = buildInsight(key);
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(11);
    const textLines = doc.splitTextToSize(insight, pageWidth - margin * 2);
    doc.text(textLines, margin, 62);

    const imageY = 90 + (textLines.length - 1) * 12;
    const imageWidth = pageWidth - margin * 2;
    const imageHeight = pageHeight - imageY - margin;

    doc.addImage(imgData, 'PNG', margin, imageY, imageWidth, imageHeight);

    const fileName = `${key}_${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.pdf`;
    doc.save(fileName);
}

function buildInsight(key) {
    const config = metricConfigs[key];
    const series = getSeriesFromPayload(key);
    if (!series || !series.length) {
        return 'Данных недостаточно для отчета.';
    }
    const points = series.flatMap((s) => s.points || []);
    if (!points.length) {
        return 'Данных недостаточно для отчета.';
    }

    const latest = getLatest(series);
    const max = points.reduce((acc, p) => (p.v > acc ? p.v : acc), points[0].v);
    const min = points.reduce((acc, p) => (p.v < acc ? p.v : acc), points[0].v);

    const latestText = latest == null ? '—' : config.formatKpi(latest);
    const maxText = config.formatKpi(max);
    const minText = config.formatKpi(min);
    const rangeText = rangeLabel(minutes);

    return `Диапазон: последние ${rangeText}. Текущее значение: ${latestText}. Пик: ${maxText}. Минимум: ${minText}.`;
}

function getSeriesFromPayload(key) {
    if (!lastPayload || typeof lastPayload !== 'object') return null;
    if (key === '5xx') return lastPayload.err5 || lastPayload.err5xx || lastPayload['5xx'] || [];
    if (key === 'active') return lastPayload.act || lastPayload.active || [];
    return lastPayload[key] || [];
}

function rangeLabel(value) {
    if (value >= 1440) return '24 часа';
    if (value >= 360) return '6 часов';
    if (value >= 60) return '1 час';
    return '15 минут';
}

function getLatest(series) {
    if (!series || !series.length) {
        return null;
    }
    let last = null;
    series.forEach((item) => {
        (item.points || []).forEach((p) => {
            if (!last || p.t > last.t) {
                last = p;
            }
        });
    });
    return last ? last.v : null;
}

function formatCompact(value) {
    if (value == null || Number.isNaN(value)) return '—';
    if (Math.abs(value) >= 1000000) return `${(value / 1000000).toFixed(1)}M`;
    if (Math.abs(value) >= 1000) return `${(value / 1000).toFixed(1)}k`;
    if (Math.abs(value) >= 100) return value.toFixed(0);
    if (Math.abs(value) >= 10) return value.toFixed(1);
    return value.toFixed(2);
}

function formatDuration(value) {
    if (value == null || Number.isNaN(value)) return '—';
    if (value >= 1000) return `${(value / 1000).toFixed(2)}s`;
    return `${value.toFixed(0)}ms`;
}

function formatBytes(value, decimals = 1) {
    if (value == null || Number.isNaN(value)) return '—';
    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    let index = 0;
    let v = Math.abs(value);
    while (v >= 1024 && index < units.length - 1) {
        v /= 1024;
        index += 1;
    }
    const scaled = value / Math.pow(1024, index);
    return `${scaled.toFixed(decimals)} ${units[index]}`;
}

function formatTime(value) {
    const date = new Date(Number(value));
    if (Number.isNaN(date.getTime())) return '';
    return date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
}

function formatNow() {
    return new Date().toLocaleString('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function setStatus(text) {
    const el = document.querySelector('[data-ops-status]');
    if (el) {
        el.textContent = text;
    }
}

function updateRangeLabel() {
    const el = document.querySelector('[data-ops-range]');
    if (!el) return;
    if (minutes >= 1440) el.textContent = '24 часа';
    else if (minutes >= 360) el.textContent = '6 часов';
    else if (minutes >= 60) el.textContent = '1 час';
    else el.textContent = '15 минут';
}

function normalizeBase(base) {
    if (!base) return '';
    return base.endsWith('/') ? base : `${base}/`;
}

function clampMinutes(value) {
    if (value < 5) return 5;
    if (value > 1440) return 1440;
    return value;
}

function toMs(value) {
    if (value > 1000000000000) {
        return value;
    }
    return value * 1000;
}

function hexToRgba(hex, alpha) {
    const raw = hex.replace('#', '');
    const bigint = parseInt(raw, 16);
    const r = (bigint >> 16) & 255;
    const g = (bigint >> 8) & 255;
    const b = bigint & 255;
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}
