namespace WebApp;
public static class Utils
{
    // Read all mock users from file
    private static readonly Arr mockUsers = JSON.Parse(
        File.ReadAllText(FilePath("json", "mock-users.json"))
    );

    // Read all bad words from file and sort from longest to shortest
    // if we didn't sort we would often get "---ing" instead of "---" etc.
    // (Comment out the sort - run the unit tests and see for yourself...)
    private static readonly Arr badWords = ((Arr)JSON.Parse(
        File.ReadAllText(FilePath("json", "bad-words.json"))
    )).Sort((a, b) => ((string)b).Length - ((string)a).Length);

    public static bool IsPasswordGoodEnough(string password)
    {
        return password.Length >= 8
            && password.Any(Char.IsDigit)
            && password.Any(Char.IsLower)
            && password.Any(Char.IsUpper)
            && password.Any(x => !Char.IsLetterOrDigit(x));
    }

    public static bool IsPasswordGoodEnoughRegexVersion(string password)
    {
        // See: https://dev.to/rasaf_ibrahim/write-regex-password-validation-like-a-pro-5175
        var pattern = @"^(?=.*[0-9])(?=.*[a-zåäö])(?=.*[A-ZÅÄÖ])(?=.*\W).{8,}$";
        return Regex.IsMatch(password, pattern);
    }

    public static string RemoveBadWords(string comment, string replaceWith = "---")
    {
        comment = " " + comment;
        replaceWith = " " + replaceWith + "$1";
        badWords.ForEach(bad =>
        {
            var pattern = @$" {bad}([\,\.\!\?\:\; ])";
            comment = Regex.Replace(
                comment, pattern, replaceWith, RegexOptions.IgnoreCase);
        });
        return comment[1..];
    }

    public static Arr CreateMockUsers()
    {
        Arr successFullyWrittenUsers = Arr();
        foreach (var user in mockUsers)
        {
            // user.password = "12345678";
            var result = SQLQueryOne(
                @"INSERT INTO users(firstName,lastName,email,password)
                VALUES($firstName, $lastName, $email, $password)
            ", user);

            if (!result.HasKey("error"))
            {
                // The specification says return the user list without password
                user.Delete("password");
                successFullyWrittenUsers.Push(user);
            }
        }
        return successFullyWrittenUsers;
    }

    public static Arr RemoveMockUsers()
    {
    // Lista för att hålla de användare som faktiskt togs bort
    Arr successfullyRemovedUsers = new Arr();

    foreach (var user in mockUsers)
    {
        // Kontrollera om användaren finns i databasen
        var result = SQLQueryOne(
            "SELECT * FROM users WHERE email = $email",
            new { email = user.email }
        );

        // Om användaren finns i databasen, ta bort användaren
        if (result != null && !result.HasKey("error"))
        {
            // Ta bort användaren
            var deleteResult = SQLQueryOne(
                "DELETE FROM users WHERE email = $email RETURNING firstName, lastName, email",
                new { email = user.email }
            );

            // Om borttagningen lyckades, lägg till användaren i listan
            if (deleteResult != null && !deleteResult.HasKey("error"))
            {
                successfullyRemovedUsers.Push(deleteResult);
            }
        }
    }

    return successfullyRemovedUsers;
    }

    public static Obj EmailDomainCounter(){
        Utils.CreateMockUsers();
        var resultFromDb = SQLQuery("SELECT * FROM emailDomainCounter ORDER BY counter DESC");
        var result = Obj();
        resultFromDb.ForEach(row => result[row.domain] = row.counter );
        Utils.RemoveMockUsers();
        Log("Check domain results", result);
        return result;
    }

}