public static readonly List<Product> products = new()
        {
            new Product
            {
                Id = 1,
                Name = "Product 1",
                Price = 100,
                Description = "Sản phẩm số 1",
                CategoryId = 1
            },
            new Product
            {
                Id = 2,
                Name = "Product 2",
                Price = 200,
                Description = "Sản phẩm số 2",
                CategoryId = 2
            },
            new Product
            {
                Id = 3,
                Name = "Product 3",
                Price = 300,
                Description = "Sản phẩm số 3",
                CategoryId = 1
            }
        };

private const int PageSize = 3;

// GET: /Product
public IActionResult Index(
    string? searchString,
    int page = 1
)
{
    lock (CategoryController.DataLock)
    {
        // Tạo bản sao và lấy thông tin danh mục.
        var productList = products
            .Select(CopyProduct)
            .ToList();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            string keyword = searchString.Trim();

            productList = productList
                .Where(p =>
                    p.Id.ToString().Contains(keyword) ||

                    p.Name.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase
                    ) ||

                    (p.Description?.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase
                    ) ?? false) ||

                    (p.Category?.Name.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase
                    ) ?? false)
                )
                .ToList();
        }

        int totalProducts = productList.Count;

        int totalPages = Math.Max(
            1,
            (int)Math.Ceiling(
                (double)totalProducts / PageSize
            )
        );

        page = Math.Clamp(page, 1, totalPages);

        var pagedProducts = productList
            .OrderBy(p => p.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        ViewBag.CurrentFilter = searchString;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;

        return View(pagedProducts);
    }
}

// GET: /Product/Details/1
public IActionResult Details(int id)
{
    lock (CategoryController.DataLock)
    {
        var product = FindProduct(id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }
}

// GET: /Product/Create
[HttpGet]
public IActionResult Create()
{
    lock (CategoryController.DataLock)
    {
        LoadCategories();
    }

    return View(new Product());
}

// POST: /Product/Create
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Create(
    [Bind("Name,Price,Description,CategoryId")]
            Product product
)
{
    lock (CategoryController.DataLock)
    {
        ValidateCategory(product.CategoryId);

        if (!ModelState.IsValid)
        {
            LoadCategories(product.CategoryId);

            return View(product);
        }

        product.Id = products.Any()
            ? products.Max(p => p.Id) + 1
            : 1;

        products.Add(product);
    }

    TempData["Success"] = "Thêm sản phẩm thành công";

    return RedirectToAction(nameof(Index));
}

// GET: /Product/Edit/1
[HttpGet]
public IActionResult Edit(int id)
{
    lock (CategoryController.DataLock)
    {
        var product = FindProduct(id);

        if (product == null)
        {
            return NotFound();
        }

        LoadCategories(product.CategoryId);

        return View(product);
    }
}

// POST: /Product/Edit/1
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Edit(
    int id,
    [Bind("Id,Name,Price,Description,CategoryId")]
            Product product
)
{
    if (id != product.Id)
    {
        return BadRequest();
    }

    lock (CategoryController.DataLock)
    {
        var existingProduct = products
            .FirstOrDefault(p => p.Id == id);

        if (existingProduct == null)
        {
            return NotFound();
        }

        ValidateCategory(product.CategoryId);

        if (!ModelState.IsValid)
        {
            LoadCategories(product.CategoryId);

            return View(product);
        }

        existingProduct.Name = product.Name;
        existingProduct.Price = product.Price;
        existingProduct.Description = product.Description;
        existingProduct.CategoryId = product.CategoryId;
    }

    TempData["Success"] = "Sửa sản phẩm thành công";

    return RedirectToAction(nameof(Index));
}

// GET: /Product/Delete/1
[HttpGet]
public IActionResult Delete(int id)
{
    lock (CategoryController.DataLock)
    {
        var product = FindProduct(id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }
}

// POST: /Product/Delete/1
[HttpPost]
[ActionName("Delete")]
[ValidateAntiForgeryToken]
public IActionResult DeleteConfirmed(int id)
{
    lock (CategoryController.DataLock)
    {
        var product = products
            .FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        products.Remove(product);
    }

    TempData["Success"] = "Xóa sản phẩm thành công";

    return RedirectToAction(nameof(Index));
}

private void LoadCategories(int? selectedId = null)
{
    ViewBag.Categories = new SelectList(
        CategoryController.categories.ToList(),
        "Id",
        "Name",
        selectedId
    );
}

private void ValidateCategory(int categoryId)
{
    bool categoryExists = CategoryController.categories
        .Any(c => c.Id == categoryId);

    if (!categoryExists)
    {
        ModelState.AddModelError(
            nameof(Product.CategoryId),
            "Vui lòng chọn danh mục hợp lệ"
        );
    }
}

private static Product? FindProduct(int id)
{
    var product = products
        .FirstOrDefault(p => p.Id == id);

    return product == null
        ? null
        : CopyProduct(product);
}

private static Product CopyProduct(Product product)
{
    var category = CategoryController.categories
        .FirstOrDefault(c => c.Id == product.CategoryId);

    return new Product
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        Description = product.Description,
        CategoryId = product.CategoryId,

        Category = category == null
            ? null
            : new Category
            {
                Id = category.Id,
                Name = category.Name
            }
    };
}