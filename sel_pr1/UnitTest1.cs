using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace sel_pr1;

public class Tests
{
    public IWebDriver driver;
    public ChromeOptions options;
    public WebDriverWait wait;
    public DateTime timeNow; //текущая дата и время для теста комментария
    
    //ссылки
    private const string urlMainPage = "https://staff-testing.testkontur.ru/";
    private const string urlCommunitiesPage = "https://staff-testing.testkontur.ru/communities";
    private const string urlDevToolsDiscussion = "https://staff-testing.testkontur.ru/communities/612a7485-7f49-48c9-8fe1-ee49b4435111?tab=discussions&id=66892117-a81f-4b3a-9e64-e09cedc18dc2";
    private const string urlProfileEditPage = "https://staff-testing.testkontur.ru/profile/settings/edit";
    //логин пароль
    private const string login = "ponomarev.egik@yandex.ru";
    private const string password = "9Ko73_PnTT88";

    [SetUp]
    public void Setup()
    {
        options = new ChromeOptions();
        options.AddArguments("--no-sandbox","--start-maximized","--disable-extensions");
        driver = new ChromeDriver(options); //запуск браузера Chrome
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30); //неявное ожидание
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30)); //явное ожидание
        //из-за моего инета ожидания пришлось сделать подольше, даже 15 секунд иногда не хватает 
    }

    [TearDown]
    public void TearDown()
    {
        driver.Quit();
        driver.Dispose();
    }

    private void Authorize(string inputLogin, string inputPassword) //метод авторизации
    {
        driver.Navigate().GoToUrl(urlMainPage); //переход на страницу
        //надо подумать как скрыть личные данные из кода
        var inputFormLogin = driver.FindElement(By.Id("Username")); //найти поле логина     
        inputFormLogin.SendKeys(inputLogin);// ввести логин
        var inputFormPassword = driver.FindElement(By.Id("Password")); //найти поле пароля
        inputFormPassword.SendKeys(inputPassword);// ввести пароль  

        var buttonEnter = driver.FindElement(By.Name("button")); //поиск кнопки "войти"
        buttonEnter.Click(); //клик войти

        //явное ожидание появления заголовка
        wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("[data-tid='Title']")));
    }

    [Test]
    public void Authorization_Test() // тест авторизации
    {
        Authorize(login, password);
        Assert.That(driver.Title, Does.Contain("Новости"), 
            "На главной странице не найден заголовок Новости"); //проверка что открыта нужная страница
    }

    [Test]
    public void NavigationMenuElement_Test() //тест перехода в меню "Сообщества"
    {
        Authorize(login, password);
        try //пытаемся найти кнопку боковой панели
        {
            var SidebarMenuButton = driver.FindElement(By.CssSelector("[data-tid='SidebarMenuButton']")); //кнопка открытия меню
            SidebarMenuButton.Click(); //клик по кнопке
            wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-tid='SidePage__root']"))); //ожидание боковой панели
            var community = driver.FindElements(By.CssSelector("[data-tid='Community'][href='/communities']")).Last(); //получаем последний элемент с данным локатором
            //не смог найти другой способ как отделить кнопку "Сообщество" внутри боковой панели
            community.Click();
        }

        catch(ElementNotInteractableException) //если не нашли кнопку меню 
        {
            TestContext.Out.WriteLine("Кнопка боковой панели не найдена");
            var community = driver.FindElement(By.CssSelector("[data-tid='Community'][href='/communities']")); //получение элемента "сообщество" 
            community.Click();
        }

        wait.Until(ExpectedConditions.UrlToBe(urlCommunitiesPage)); //ожидание перехода на нужную страницу
        var titlePageElement = driver.FindElement(By.CssSelector("[data-tid='Title']")); //поиск заголовка

        Assert.That(titlePageElement.Text, Does.Contain("Сообщества"), //проверка заголовка
            "При переходе на вкладку Сообщества не был найден заголовок Сообщества");
    }

    [Test]
    public void FindingPersonUsingSearchBar_Test() //тест поисковой строки
    {
        Authorize(login, password);

        var search = driver.FindElement(By.CssSelector("[data-tid='SearchBar']")); //поиск строки поиска
        search.Click();

        var searchInput = driver.FindElement(By.CssSelector("[placeholder='Поиск сотрудника, подразделения, сообщества, мероприятия']"));
        searchInput.SendKeys("пономарев егор павлович"); //ввод запроса

        Assert.That(searchInput.GetAttribute("value"), Does.Contain("пономарев егор павлович"),
            "Поле поиска должно содержать введённое значение");
    }

    [Test]
    public void SendComment_Test() //тест отправки комментария (к сожалению удалить можно только через апишку)
    {
        timeNow = DateTime.Now; //дата и время для уникальности комментариев
        var stringComment = $"пономарев автотест комент {timeNow.ToString("G")}"; //текст комента

        Authorize(login, password);
        driver.Navigate().GoToUrl(urlDevToolsDiscussion);
        wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[placeholder='Комментировать...']")));
        driver.FindElement(By.CssSelector("[placeholder='Комментировать...']")).Click(); //кликаем на поле ввода комента 

        var formInputComment = driver.FindElement(By.CssSelector("[data-tid='CommentInput']"));
        formInputComment.SendKeys(stringComment);

        var buttonSendComment = driver.FindElement(By.CssSelector("[data-tid='SendComment']"));
        buttonSendComment.Click();
        // явно ждём пока не пропадёт кнопка "Отправить"
        wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector("[data-tid='SendComment']")));

        var TextComment = driver.FindElements(By.CssSelector("[data-tid='TextComment']")).Last();
        Assert.That(TextComment.GetAttribute("textContent"), Does.Contain(stringComment),
            "Текст последнего комментария в списке отличается от введённого");
    }

    [Test]
    public void EditProfileDescription_Test()
    {
        timeNow = DateTime.Now;
        var stringAddress = $"Автотест редактирование адреса {timeNow.ToString("G")}";
        Authorize(login, password);
        driver.Navigate().GoToUrl(urlProfileEditPage);
        wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("[data-tid='Title']")));

        var fieldAddress = driver.FindElement(By.CssSelector("[data-tid='Address']"));
        var formAddress = fieldAddress.FindElement(By.CssSelector("[data-tid='Input']"));
        //formAddress.Clear(); почему-то перестал работать метод Clear, пришлось через сочетание клавиш
        formAddress.SendKeys(Keys.Control + "a");
        formAddress.SendKeys(Keys.Delete);
        formAddress.SendKeys(stringAddress);

        var buttonSaveChanges = driver.FindElement(By.XPath("//button[text()='Сохранить']")); //ищем кнопку "Сохранить"
        buttonSaveChanges.Click();

        wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("[data-tid='ContactCard']")));
        //проверяем что адрес отображаемый на странице нашего профиля свопадает с введённым в тесте
        var readProfileAddress =  driver.FindElement(By.CssSelector("[class='sc-bdnxRM sc-jcwpoC sc-iTVJFM kSMcXF iUFNtI']"));
        Assert.That(readProfileAddress.GetAttribute("textContent"), 
            Does.Contain(stringAddress), "Адрес показанный в профиле не совпадает с введённым");
    }
}
