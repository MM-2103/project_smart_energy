// Chart.js integration for Blazor
window.chartFunctions = {
    createLineChart: function(canvasId, chartData) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        
        const chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: chartData.labels,
                datasets: [{
                    label: chartData.dataset1Label,
                    data: chartData.dataset1Data,
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1
                }, {
                    label: chartData.dataset2Label,
                    data: chartData.dataset2Data,
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.2)',
                    tension: 0.1
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: chartData.title
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: chartData.yAxisLabel
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: chartData.xAxisLabel
                        }
                    }
                }
            }
        });
        
        return chart;
    },
    
    createBarChart: function(canvasId, chartData) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        
        const chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: chartData.labels,
                datasets: [{
                    label: chartData.datasetLabel,
                    data: chartData.data,
                    backgroundColor: 'rgba(54, 162, 235, 0.2)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: chartData.title
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: chartData.yAxisLabel
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: chartData.xAxisLabel
                        }
                    }
                }
            }
        });
        
        return chart;
    },
    
    destroyChart: function(chart) {
        if (chart) {
            chart.destroy();
        }
    }
};