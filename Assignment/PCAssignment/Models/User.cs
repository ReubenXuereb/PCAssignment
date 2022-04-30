using Google.Cloud.Firestore;

namespace PCAssignment.Models
{
    [FirestoreData]
    public class User
    {
    
        [FirestoreProperty]
        public string FirstName { get; set; }

        [FirestoreProperty]
        public string LastName { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string MobileNum { get; set; }

        [FirestoreProperty]
        public int AvailableCredits { get; set; }


    }
}
