import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Toast } from '../../../core/toast';
import { printBarcode } from '../../../shared/utils/barcode-print';
import { BrandDto } from '../../master-data/brand/brand.model';
import { BrandService } from '../../master-data/brand/brand.service';
import { CategoryDto } from '../../master-data/category/category.model';
import { CategoryService } from '../../master-data/category/category.service';
import { UnitDto } from '../../master-data/unit/unit.model';
import { UnitService } from '../../master-data/unit/unit.service';
import { ProductDto } from '../product.model';
import { ProductService } from '../product.service';

/**
 * Category/Brand/Unit dropdowns are populated via a single pageSize=100 fetch, not a dedicated
 * "list all" endpoint - a pragmatic MVP limit (shared PagedRequest's own validator caps pageSize at 100
 * anyway) rather than building unpaginated list endpoints for master data too. Revisit if a shop ever
 * has more than 100 of any of these.
 */
@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly brandService = inject(BrandService);
  private readonly unitService = inject(UnitService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly uploadingImage = signal(false);
  protected readonly productId = signal<number | null>(null);
  protected readonly barcode = signal<string | null>(null);
  protected readonly imageUrl = signal<string | null>(null);
  protected readonly categories = signal<CategoryDto[]>([]);
  protected readonly brands = signal<BrandDto[]>([]);
  protected readonly units = signal<UnitDto[]>([]);

  protected readonly isEditMode = () => this.productId() !== null;

  protected readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    barcode: [''],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    categoryId: [0, Validators.required],
    brandId: [null as number | null],
    unitId: [0, Validators.required],
    purchasePrice: [0, [Validators.required, Validators.min(0)]],
    sellingPrice: [0, [Validators.required, Validators.min(0)]],
    wholesalePrice: [null as number | null],
    discountPercentage: [0, [Validators.min(0), Validators.max(100)]],
    taxPercentage: [0, [Validators.min(0), Validators.max(100)]],
    currentStock: [0, [Validators.min(0)]],
    minimumStock: [0, [Validators.min(0)]],
    allowPriceBelowCost: [false]
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam ? Number(idParam) : null;
    this.productId.set(id);

    // currentStock is only settable at creation - see the backend's Module 4 Step 1 scope decision.
    if (id !== null) {
      this.form.controls.currentStock.disable();
    }

    this.loadDropdownData();

    if (id !== null) {
      this.loadProduct(id);
    } else {
      this.loading.set(false);
    }
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const raw = this.form.getRawValue();
    const request = { ...raw, barcode: raw.barcode || null };

    const id = this.productId();
    const result$ = id === null ? this.productService.create(request) : this.productService.update(id, request);

    result$.subscribe({
      next: (product) => {
        this.saving.set(false);
        this.toast.success('Product saved.');

        if (id === null) {
          // Land on the edit page - that's where image upload and barcode printing become available.
          this.router.navigateByUrl(`/products/${product.id}/edit`);
        } else {
          this.applyProduct(product);
        }
      },
      error: (error) => {
        this.saving.set(false);
        this.toast.error(error.error?.title ?? 'Could not save product.');
      }
    });
  }

  protected onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const id = this.productId();
    if (!file || id === null) {
      return;
    }

    this.uploadingImage.set(true);
    this.productService.uploadImage(id, file).subscribe({
      next: (product) => {
        this.uploadingImage.set(false);
        this.applyProduct(product);
        this.toast.success('Image updated.');
      },
      error: () => {
        this.uploadingImage.set(false);
        this.toast.error('Could not upload image.');
      }
    });

    input.value = '';
  }

  protected printBarcode(): void {
    const value = this.barcode();
    if (value) {
      printBarcode(value, this.form.controls.name.value);
    }
  }

  private loadDropdownData(): void {
    this.categoryService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.categories.set(result.items));
    this.brandService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.brands.set(result.items));
    this.unitService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe((result) => this.units.set(result.items));
  }

  private loadProduct(id: number): void {
    this.productService.getById(id).subscribe({
      next: (product) => {
        this.applyProduct(product);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Could not load product.');
      }
    });
  }

  private applyProduct(product: ProductDto): void {
    this.barcode.set(product.barcode);
    this.imageUrl.set(product.imageUrl);
    this.form.patchValue({
      code: product.code,
      barcode: product.barcode,
      name: product.name,
      categoryId: product.categoryId,
      brandId: product.brandId,
      unitId: product.unitId,
      purchasePrice: product.purchasePrice,
      sellingPrice: product.sellingPrice,
      wholesalePrice: product.wholesalePrice,
      discountPercentage: product.discountPercentage,
      taxPercentage: product.taxPercentage,
      currentStock: product.currentStock,
      minimumStock: product.minimumStock,
      allowPriceBelowCost: product.allowPriceBelowCost
    });
  }
}
