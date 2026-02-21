using Workshop.Models;

namespace Workshop.Services;

public sealed class CustomerBookingProfileService
{
    public CustomerProfile BuildProfileFromBooking(
        Booking booking,
        IReadOnlyCollection<CustomerProfile> existingProfiles)
    {
        var existing = CustomerProfileCoreService.FindCustomerProfileCore(
            existingProfiles,
            booking.CustomerAccountNumber,
            booking.CustomerEmail,
            booking.CustomerPhone);

        var profile = existing is null
            ? new CustomerProfile
            {
                AccountNumber = string.IsNullOrWhiteSpace(booking.CustomerAccountNumber)
                    ? CustomerProfileCoreService.GenerateNextCustomerAccountNumber(existingProfiles)
                    : booking.CustomerAccountNumber.Trim()
            }
            : CustomerProfileCoreService.CloneCustomerProfile(existing);

        profile.FirstName = string.IsNullOrWhiteSpace(booking.CustomerFirstName) ? profile.FirstName : booking.CustomerFirstName.Trim();
        profile.LastName = string.IsNullOrWhiteSpace(booking.CustomerLastName) ? profile.LastName : booking.CustomerLastName.Trim();
        profile.Phone = string.IsNullOrWhiteSpace(booking.CustomerPhone) ? profile.Phone : booking.CustomerPhone.Trim();
        profile.Email = string.IsNullOrWhiteSpace(booking.CustomerEmail) ? profile.Email : booking.CustomerEmail.Trim();

        var parsedBike = CustomerBikeProfileMapper.ParseBikeFromDetails(booking.BikeDetails);
        if (parsedBike is not null)
        {
            var fingerprint = CustomerBikeProfileMapper.GetFingerprint(parsedBike);
            var bikeExists = profile.Bikes.Any(b =>
                CustomerBikeProfileMapper.GetFingerprint(b).Equals(fingerprint, StringComparison.OrdinalIgnoreCase));
            if (!bikeExists)
                profile.Bikes.Add(parsedBike);
        }

        return profile;
    }
}
