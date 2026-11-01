using SalesSample.Domain.Entities;

namespace SalesSample.Domain.Specifications;

public sealed class ActiveBranchSpecification : ISpecification<Branch>
{
    public bool IsSatisfiedBy(Branch branch) => branch.IsActive;
}