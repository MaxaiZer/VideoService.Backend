using Domain.Entities;

namespace CoreService.Application.Common.Extensions;

public static class VideoParametersConverter
{
    public static VideoSearchParameters ToDomain(this Dto.VideoParameters parameters)
    {
        return new VideoSearchParameters(
            userId: parameters.UserId,
            searchQuery: parameters.SearchQuery,
            pageNumber: parameters.PageNumber,
            pageSize: parameters.PageSize
        );
    } 
}