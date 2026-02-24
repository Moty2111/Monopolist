// site.js – общие скрипты для всего приложения

document.addEventListener('DOMContentLoaded', function () {
    // Инициализация мобильного меню (для страниц дашборда)
    initMobileMenu();

    // Автоматическое скрытие алертов через 8 секунд и обработка кнопок закрытия
    initAlerts();

    // Плавный скролл для якорей (если есть)
    initSmoothScroll();
});

/**
 * Управление мобильным меню (бургер)
 */
function initMobileMenu() {
    const menuToggle = document.querySelector('.menu-toggle');
    const body = document.body;

    if (menuToggle) {
        menuToggle.addEventListener('click', function () {
            body.classList.toggle('sidebar-open');
        });

        // Закрытие меню при клике вне сайдбара (на мобильных)
        document.addEventListener('click', function (event) {
            if (window.innerWidth <= 768) {
                const sidebar = document.querySelector('.sidebar');
                const isClickInsideSidebar = sidebar && sidebar.contains(event.target);
                const isClickOnToggle = menuToggle.contains(event.target);

                if (!isClickInsideSidebar && !isClickOnToggle && body.classList.contains('sidebar-open')) {
                    body.classList.remove('sidebar-open');
                }
            }
        });
    }
}

/**
 * Управление алертами: авто-скрытие через 8 секунд, кнопка закрытия
 */
function initAlerts() {
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');

    alerts.forEach(alert => {
        // Добавляем кнопку закрытия, если её нет
        if (!alert.querySelector('.btn-close')) {
            const closeBtn = document.createElement('button');
            closeBtn.type = 'button';
            closeBtn.className = 'btn-close';
            closeBtn.setAttribute('aria-label', 'Закрыть');
            closeBtn.innerHTML = '&times;';
            alert.appendChild(closeBtn);
        }

        // Обработчик на кнопку закрытия
        const closeBtn = alert.querySelector('.btn-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', function () {
                fadeOut(alert, 300);
            });
        }

        // Авто-скрытие через 8 секунд (кроме ошибок валидации – они не авто-скрываются)
        if (!alert.classList.contains('validation-summary')) {
            setTimeout(() => {
                if (alert.parentNode) {
                    fadeOut(alert, 500);
                }
            }, 8000);
        }
    });
}

/**
 * Плавное исчезновение элемента
 */
function fadeOut(element, duration) {
    element.style.transition = `opacity ${duration}ms ease`;
    element.style.opacity = '0';
    setTimeout(() => {
        if (element.parentNode) {
            element.style.display = 'none';
        }
    }, duration);
}

/**
 * Плавный скролл к якорям
 */
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href === '#') return;
            const target = document.querySelector(href);
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });
}

/**
 * Специфичные для страницы входа действия
 */
function initLoginPage() {
    // Дополнительная обработка для страницы входа (если нужно)
    console.log('Login page initialized');
}