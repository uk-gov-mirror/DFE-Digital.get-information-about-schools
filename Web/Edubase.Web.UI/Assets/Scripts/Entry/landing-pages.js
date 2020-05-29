import GiasTableSort from "../GiasModules/GiasTableSort";
import GiasTabs from "../GiasModules/GiasTabs";
import giasDismissMessage from "../GiasModules/GiasDismissMessage";
import GiasLandingMap from "../GiasLandingPages/GiasLandingMap";

const $main = $('#main-content');

const $map = $('#map');
const mobileToggleSwitch = $('#map-toggle');
let mapInitialised = false;

$main.find('.gias-tabs-wrapper').giasTabs();

$('#school-governance').find('.sortable-table').giasTableSort();

if ($map.length && $map.css('display') === 'block') {
  new GiasLandingMap();
  mapInitialised = true;
} else {
  mobileToggleSwitch.on('click', function(e) {
    e.preventDefault();
    if (mobileToggleSwitch.hasClass('trigger-open')) {
      $map.css({ display: 'none' });
      $(this).text('Show map');

    } else {
      $map.css({ display: 'block' });
      $(this).text('Close map');
      if (!mapInitialised) {
        new GiasLandingMap();
        mapInitialised = true;
      }
    }
    mobileToggleSwitch.toggleClass('trigger-open');
  });
}

giasDismissMessage();
