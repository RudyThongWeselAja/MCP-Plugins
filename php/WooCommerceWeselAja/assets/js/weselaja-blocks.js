(function (wp, wc) {
    if (!wc || !wc.wcBlocksRegistry || !wc.wcSettings) {
        return;
    }

    var settings = wc.wcSettings.getSetting('weselaja_data', {});
    var createElement = wp.element.createElement;
    var label = settings.title || 'WeselAja Virtual Account';
    var description = settings.description || '';
    var Content = function () {
        return createElement('div', null, description);
    };

    wc.wcBlocksRegistry.registerPaymentMethod({
        name: 'weselaja',
        label: createElement('span', null, label),
        content: createElement(Content),
        edit: createElement(Content),
        canMakePayment: function () {
            return true;
        },
        ariaLabel: label,
        supports: {
            features: settings.supports || ['products'],
        },
    });
})(window.wp, window.wc);
