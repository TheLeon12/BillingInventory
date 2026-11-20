using BillingInventory.Application.Common.Paging;
using BillingInventory.Domain.Common;
using FluentValidation;
using Shouldly;
using SortDirection = BillingInventory.Application.Common.Paging.SortDirection;

namespace BillingInventory.UnitTests.Paging;

public sealed class PagingTests
{
    private sealed class Item : BaseEntity
    {
        public string Name { get; set; } = "";

        public decimal Price { get; set; }
    }

    private sealed class ItemQueryValidator : PagedQueryValidator<PagedQuery>;

    private static readonly SortMap<Item> Map = new SortMap<Item>("name")
        .Add("name", i => i.Name)
        .Add("price", i => i.Price);

    private static IQueryable<Item> Items() => new[]
    {
        new Item { Id = 1, Name = "Banana", Price = 10m },
        new Item { Id = 2, Name = "Apple", Price = 30m },
        new Item { Id = 3, Name = "Cherry", Price = 10m },
    }.AsQueryable();

    [Fact]
    public void PagedResult_ComputesMetadata()
    {
        var result = new PagedResult<int>([1, 2, 3], Page: 2, PageSize: 3, TotalCount: 10);

        result.TotalPages.ShouldBe(4);
        result.HasPrevious.ShouldBeTrue();
        result.HasNext.ShouldBeTrue();
    }

    [Fact]
    public void PagedResult_LastPage_HasNoNext()
    {
        var result = new PagedResult<int>([1], Page: 4, PageSize: 3, TotalCount: 10);

        result.HasNext.ShouldBeFalse();
    }

    [Fact]
    public void PagedResult_Empty_HasZeroPages()
    {
        var result = new PagedResult<int>([], Page: 1, PageSize: 20, TotalCount: 0);

        result.TotalPages.ShouldBe(0);
        result.HasNext.ShouldBeFalse();
        result.HasPrevious.ShouldBeFalse();
    }

    [Fact]
    public void SortMap_WithoutSortBy_UsesDefaultField()
    {
        var names = Map.Apply(Items(), null, null).Select(i => i.Name).ToList();

        names.ShouldBe(["Apple", "Banana", "Cherry"]);
    }

    [Fact]
    public void SortMap_Descending_ReversesOrder()
    {
        var names = Map.Apply(Items(), "name", SortDirection.Desc).Select(i => i.Name).ToList();

        names.ShouldBe(["Cherry", "Banana", "Apple"]);
    }

    [Fact]
    public void SortMap_IsCaseInsensitive()
    {
        var names = Map.Apply(Items(), "PRICE", SortDirection.Desc).Select(i => i.Name).ToList();

        names[0].ShouldBe("Apple");
    }

    [Fact]
    public void SortMap_TiesAreBrokenById()
    {
        // Banana (Id 1) y Cherry (Id 3) tienen el mismo precio: siempre deben salir en el mismo orden.
        var ids = Map.Apply(Items(), "price", SortDirection.Asc).Select(i => i.Id).ToList();

        ids.ShouldBe([1, 3, 2]);
    }

    [Fact]
    public void SortMap_UnknownField_ThrowsValidationExceptionListingAllowedFields()
    {
        var ex = Should.Throw<ValidationException>(() => Map.Apply(Items(), "password", null));

        ex.Errors.ShouldContain(e => e.PropertyName == "sortBy");
        ex.Message.ShouldContain("name");
        ex.Message.ShouldContain("price");
    }

    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    [InlineData(PagedQuery.MaxPage + 1, 20, false)]
    public void Validator_EnforcesPageLimits(int page, int pageSize, bool valid)
    {
        var result = new ItemQueryValidator().Validate(new PagedQuery { Page = page, PageSize = pageSize });

        result.IsValid.ShouldBe(valid);
    }

    [Fact]
    public void WhereIf_AppliesFilterOnlyWhenConditionIsTrue()
    {
        Items().WhereIf(false, i => i.Price > 20m).Count().ShouldBe(3);
        Items().WhereIf(true, i => i.Price > 20m).Count().ShouldBe(1);
    }
}
