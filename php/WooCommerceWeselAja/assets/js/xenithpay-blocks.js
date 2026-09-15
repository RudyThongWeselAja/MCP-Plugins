( function( wp, wc ) {
    console.log('WC XenithPay Blocks: Script loaded.');

    if ( ! wc || ! wc.wcBlocksRegistry ) {
        console.error( 'WC XenithPay Blocks: wc.wcBlocksRegistry not found.' );
        return;
    }

    var registerPaymentMethod = wc.wcBlocksRegistry.registerPaymentMethod;
    var getSetting = wc.wcSettings.getSetting;
    var createElement = wp.element.createElement;
    var decodeEntities = wp.htmlEntities.decodeEntities;
    var __ = wp.i18n.__;

    var settings = getSetting( 'xenithpay_data', {} );

    var label = decodeEntities( settings.title || 'XenithPay' );
	
	var iconUrl = settings.icon;

	var Icon = iconUrl
	  ? createElement('img', {
		  src: iconUrl,
		  alt: label,
		  style: {
			marginLeft: '0.3em',
			maxHeight: '28px',
			maxWidth: '65px',
		  },
		})
	  : null;

    var Content = function() {
        return decodeEntities( settings.description || '' );
    };

    var Label = function () {
	  return createElement(
		'div',
		{
		  style: {
			display: 'flex',
			alignItems: 'center',
			justifyContent: 'space-between',
			width: '100%',
		  },
		},
		// Text di kiri
		createElement(
		  'span',
		  { style: { fontWeight: 500 } },
		  label
		),

		// Icon di kanan
		Icon
	  );
	};

    registerPaymentMethod( {
        name: "xenithpay",
        label: createElement( Label ),
        content: createElement( Content ),
        edit: createElement( Content ),
        canMakePayment: function() { return true; },
        ariaLabel: label,
        supports: {
            features: settings.supports,
        }
    } );
    
    console.log('WC XenithPay Blocks: Registered successfully.');
} )( window.wp, window.wc );