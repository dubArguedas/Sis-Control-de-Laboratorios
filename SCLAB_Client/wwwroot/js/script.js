window.addEventListener('DOMContentLoaded', (event) => {
    document.body.getElementsByClassName('rz-button')[0].style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.2)';
    Array.from(document.body.getElementsByClassName('rz-button')).forEach(element => {
        element.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.2)';
    });
});
