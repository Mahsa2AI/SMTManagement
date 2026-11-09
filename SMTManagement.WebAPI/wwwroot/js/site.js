document.addEventListener('DOMContentLoaded', () => {
    const tabs = document.querySelectorAll('[data-tab]');
    const panels = document.querySelectorAll('[data-panel]');

    const activateTab = tabName => {
        const selectedTab = [...tabs].find(tab => tab.dataset.tab === tabName) ?? tabs[0];
        if (!selectedTab) return;

        tabs.forEach(item => item.classList.toggle('is-active', item === selectedTab));
        panels.forEach(panel => panel.classList.toggle('is-active', panel.dataset.panel === selectedTab.dataset.tab));
        sessionStorage.setItem('smt-management-active-tab', selectedTab.dataset.tab);
    };

    tabs.forEach(tab => tab.addEventListener('click', () => activateTab(tab.dataset.tab)));
    activateTab(sessionStorage.getItem('smt-management-active-tab') ?? 'orders');

    const closeModal = modal => {
        modal.classList.remove('is-open');
        document.body.classList.remove('modal-open');
    };

    document.querySelectorAll('[data-open]').forEach(button => {
        button.addEventListener('click', () => {
            const modal = document.getElementById(button.dataset.open);
            if (modal) {
                modal.classList.add('is-open');
                document.body.classList.add('modal-open');
            }
        });
    });

    document.querySelectorAll('[data-close]').forEach(button => {
        button.addEventListener('click', () => closeModal(button.closest('.modal')));
    });

    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', event => {
            if (event.target === modal) closeModal(modal);
        });
    });

    document.querySelectorAll('[data-expand]').forEach(button => {
        button.addEventListener('click', () => {
            const detail = document.getElementById(button.dataset.expand);
            if (!detail) return;

            const expanded = button.getAttribute('aria-expanded') === 'true';
            button.setAttribute('aria-expanded', (!expanded).toString());
            button.textContent = expanded ? '+' : '−';
            detail.hidden = expanded;
        });
    });

    document.querySelectorAll('[data-filter]').forEach(input => {
        input.addEventListener('input', () => {
            const list = document.getElementById(input.dataset.filter);
            const query = input.value.toLowerCase();
            list?.querySelectorAll('.selection-item').forEach(item => {
                item.hidden = !item.textContent.toLowerCase().includes(query);
            });
        });
    });
});
