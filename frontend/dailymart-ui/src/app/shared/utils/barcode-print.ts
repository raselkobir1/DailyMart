import JsBarcode from 'jsbarcode';

/**
 * Renders `value` into a new browser tab as a scannable barcode graphic and triggers the print dialog.
 * Uses CODE128 rather than EAN13 - a user-supplied barcode isn't guaranteed to be EAN13-checksum-valid
 * (only the server's auto-generated ones are), and CODE128 encodes any string without that constraint
 * while still being scannable by standard barcode readers.
 */
export function printBarcode(value: string, label: string): void {
  const printWindow = window.open('', '_blank', 'width=400,height=300');
  if (!printWindow) {
    return;
  }

  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  JsBarcode(svg, value, { format: 'CODE128', displayValue: true });

  printWindow.document.write(
    `<html><head><title>${label}</title></head>` +
      `<body style="display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;margin:0;">` +
      `<div>${svg.outerHTML}</div><p>${label}</p></body></html>`
  );
  printWindow.document.close();
  printWindow.focus();
  printWindow.print();
}
