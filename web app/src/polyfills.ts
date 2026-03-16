/***************************************************************************************************
 * BẮT BUỘC: Polyfills cho trình duyệt cũ
 */

// Import core-js cho ES5+
import 'core-js/es/array';
import 'core-js/es/object';
import 'core-js/es/promise';
import 'core-js/es/symbol';

// Zone.js cho trình duyệt không hỗ trợ async/await
import 'zone.js';

// Polyfill cho trình duyệt không hỗ trợ fetch API
import 'whatwg-fetch';
