import JsBarcode from 'jsbarcode';

/** `label` is a product name/barcode a tenant user typed in - it ends up inside HTML built as a plain
 * string and fed to `document.write`, so it must be escaped the same way Angular's template interpolation
 * would do automatically; a name like `<img src=x onerror=...>` would otherwise execute in the print
 * window (same origin as the app, so it could read the JWT out of localStorage). */
function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

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
  const safeLabel = escapeHtml(label);

  printWindow.document.write(
    `<html><head><title>${safeLabel}</title></head>` +
      `<body style="display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;margin:0;">` +
      `<div>${svg.outerHTML}</div><p>${safeLabel}</p></body></html>`
  );
  printWindow.document.close();
  printWindow.focus();
  printWindow.print();
}

/**
 * Renders `copies` copies of the same barcode onto an A4 sheet of label cells and triggers the print
 * dialog - the Products list's "print barcodes for this product's stock" action, one sticker per
 * physical unit currently on the shelf, rather than the single graphic printBarcode() produces.
 */
export function printBarcodeSheet(value: string, label: string, copies: number): void {
  const printWindow = window.open('', '_blank');
  if (!printWindow) {
    return;
  }

  const safeLabel = escapeHtml(label);
  const cells: string[] = [];
  for (let i = 0; i < copies; i++) {
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    JsBarcode(svg, value, { format: 'CODE128', displayValue: true, width: 1.5, height: 40, fontSize: 12, margin: 4 });
    cells.push(`<div class="label-cell">${svg.outerHTML}<p>${safeLabel}</p></div>`);
  }

  printWindow.document.write(
    `<html><head><title>${safeLabel} - ${copies} labels</title><style>` +
      '@page { size: A4; margin: 10mm; }' +
      'body { margin: 0; font-family: Arial, sans-serif; }' +
      '.sheet { display: grid; grid-template-columns: repeat(3, 1fr); gap: 4mm; }' +
      '.label-cell { display: flex; flex-direction: column; align-items: center; justify-content: center; ' +
      'border: 1px dashed #999; padding: 3mm; page-break-inside: avoid; }' +
      '.label-cell svg { max-width: 100%; }' +
      '.label-cell p { margin: 1mm 0 0; font-size: 9pt; text-align: center; word-break: break-word; }' +
      `</style></head><body><div class="sheet">${cells.join('')}</div></body></html>`
  );
  printWindow.document.close();
  printWindow.focus();
  printWindow.print();
}
