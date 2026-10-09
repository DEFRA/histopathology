using Histo.Web.Pages.Admin;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="UserFormValidator"/> — the shared Add/Edit user field-validation
/// rules extracted to remove the duplicate <c>Validate()</c> method SonarCloud flagged on
/// <see cref="AddUserModel"/> and <see cref="EditUserModel"/>.
/// </summary>
public class UserFormValidatorTests
{
    [Fact]
    public void Validate_AllFieldsValid_AddsNoErrors()
    {
        var errors = new Dictionary<string, string>();

        UserFormValidator.Validate(errors, "Alice", "alice@example.com", 1, 5);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NameMissing_AddsNameError()
    {
        var errors = new Dictionary<string, string>();

        UserFormValidator.Validate(errors, "", "alice@example.com", 1, 5);

        Assert.Equal("Enter the user's name.", errors["Name"]);
    }

    [Fact]
    public void Validate_NameTooLong_AddsLengthError()
    {
        var errors = new Dictionary<string, string>();

        UserFormValidator.Validate(errors, new string('x', 36), "alice@example.com", 1, 5);

        Assert.Equal("Name must be 35 characters or less.", errors["Name"]);
    }

    [Fact]
    public void Validate_EmailMissing_AddsEmailError()
    {
        var errors = new Dictionary<string, string>();

        UserFormValidator.Validate(errors, "Alice", "", 1, 5);

        Assert.Equal("Enter the user's email.", errors["Email"]);
    }

    [Fact]
    public void Validate_EmailTooLong_AddsLengthError()
    {
        var errors = new Dictionary<string, string>();

        UserFormValidator.Validate(errors, "Alice", new string('x', 55) + "@b.com", 1, 5);

        Assert.Equal("Email must be 60 characters or less.", errors["Email"]);
    }

    [Fact]
    public void Validate_GroupNotSelected_AddsGroupError()
    {
        var errors = new Dictionary<string, string>();

        UserFormValidator.Validate(errors, "Alice", "alice@example.com", 0, 5);

        Assert.Equal("Select a user group.", errors["GroupCode"]);
    }

    [Fact]
    public void Validate_AreaNotSelected_AddsAreaError()
    {
        var errors = new Dictionary<string, string>();

        UserFormValidator.Validate(errors, "Alice", "alice@example.com", 1, 0);

        Assert.Equal("Select a user area.", errors["AreaCode"]);
    }
}
