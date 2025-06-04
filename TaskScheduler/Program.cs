using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime Deadline { get; set; }

    [Column("IsCompleted")]
    public bool IsCompleted { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=" + Path.Combine(Directory.GetCurrentDirectory(), "tasks.db"));
}

public interface ITaskRepository
{
    void AddTask(TaskItem task);
    List<TaskItem> GetAllTasks();
    void UpdateTask(TaskItem task);
    void DeleteTask(int id);
    TaskItem GetTaskById(int id);
}

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;
    public TaskRepository(AppDbContext db) => _db = db;
    public void AddTask(TaskItem task)
    {
        _db.Tasks.Add(task);
        _db.SaveChanges();
    }
    public List<TaskItem> GetAllTasks()
    {
        return _db.Tasks.ToList();
    }
    public void UpdateTask(TaskItem task)
    {
        var existingTask = _db.Tasks.Find(task.Id);
        if (existingTask != null)
        {
            existingTask.Title = task.Title;
            existingTask.Deadline = task.Deadline;
            existingTask.IsCompleted = task.IsCompleted;

            _db.SaveChanges();
        }
        else
        {
            throw new ArgumentException("Задача не найдена");
        }
    }
    public void DeleteTask(int id)
    {
        var taskToDelete = _db.Tasks.Find(id);
        if (taskToDelete != null)
        {
            _db.Tasks.Remove(taskToDelete);
            _db.SaveChanges();
        }
        else
        {
            throw new ArgumentException("Задача не найдена");
        }
    }
    public TaskItem GetTaskById(int id)
    {
        return _db.Tasks.FirstOrDefault(t => t.Id == id);
    }
}

public class Program
{
    static void Main(string[] args)
    {
        using (var db = new AppDbContext())
        {
            bool isRunning = true;

            ITaskRepository repository = new TaskRepository(db);

            while (isRunning)
            {

                Console.WriteLine($"1. Добавить задачу \n2. Вывести список задач \n3. Обновить задачу \n4. Удалить задачу \n5. Выйти");
                Console.WriteLine("\nВыберете опцию: ");
                int selector = Convert.ToInt32(Console.ReadLine());
                switch (selector)
                {
                    case 1:
                        Console.WriteLine("Введите задачу: ");
                        string title = Console.ReadLine();
                        Console.WriteLine("Введите дедлайн в формате ГГГГ-ММ-ДД: ");
                        DateTime deadline = DateTime.Parse(Console.ReadLine());

                        repository.AddTask(new TaskItem
                        {
                            Title = title,
                            Deadline = deadline
                        });
                        break;

                    case 2:
                        Console.WriteLine("Список задач: ");

                        var tasks = repository.GetAllTasks();

                        if (tasks.Count == 0)
                        {
                            Console.WriteLine("Нет задач для отображения");
                        }
                        else
                        {
                            foreach (var task in tasks)
                            {
                                Console.WriteLine($"\nID: {task.Id}");
                                Console.WriteLine($"Задача: {task.Title}");
                                Console.WriteLine($"Дедлайн: {task.Deadline}");
                                Console.WriteLine($"Статус: {(task.IsCompleted ? "Выполнена" : "В работе")}");
                            }
                        }
                        break;

                    case 3:
                        Console.WriteLine("Обновление задачи");

                        var uTasks = repository.GetAllTasks();
                        if (uTasks.Count == 0)
                        {
                            Console.WriteLine("Нет задач для редактирования!");
                            break;
                        }
                        foreach (var task in uTasks)
                        {
                            Console.WriteLine($"ID: {task.Id} \nЗадача: {task.Title} \nДедлайн: {task.Deadline:yyyy-MM-dd} \nСтатус: {(task.IsCompleted ? "Выполнена" : "В работе")}");
                        }
                        Console.WriteLine("Введите ID задачи для редактирования: ");
                        if (!int.TryParse(Console.ReadLine(), out int uTaskId))
                        {
                            Console.WriteLine("Неверный формат ID");
                            break;
                        }
                        var existingTask = repository.GetTaskById(uTaskId);
                        if (existingTask == null)
                        {
                            Console.WriteLine($"Задача с ID - {uTaskId} не найдена");
                            break;
                        }

                        var originalTitle = existingTask.Title;
                        var originalDeadline = existingTask.Deadline;
                        var originalStatus = existingTask.IsCompleted;

                        Console.WriteLine($"Текущие значения: \n1. Название: {existingTask.Title} \n2. Дедлайн: {existingTask.Deadline:yyyy-MM-dd} \n3. Статус: {(existingTask.IsCompleted ? "Выполнена" : "В работе")}");
                        Console.WriteLine("Введите новые значения (оставте пустым, чтобы не менять)");
                        Console.WriteLine("Новое название: ");
                        string newTitle = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(newTitle))
                        {
                            existingTask.Title = newTitle;
                        }
                        Console.WriteLine("Введите новый дедлайн (ГГГГ-ММ-ДД): ");
                        string newDeadline = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(newDeadline))
                        {
                            if (DateTime.TryParse(newDeadline, out DateTime parsedDeadline))
                            {
                                existingTask.Deadline = parsedDeadline;
                            }
                            else
                            {
                                Console.WriteLine("Неверный формат даты! Значение не измененою.");
                            }
                        }
                        Console.WriteLine("Статус y - выполнена, n - не выполнена");
                        string statusInput = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(statusInput))
                        {
                            existingTask.IsCompleted = statusInput.ToLower() == "y";
                        }

                        Console.WriteLine($"Новые значения: \nНазвание: {existingTask.Title} \nДедлайн: existingTask.Deadline:yyyy-MM-dd \n Статус: {(existingTask.IsCompleted ? "Выполнена" : "Не выполнена")}");

                        Console.WriteLine("Подтвердить изменения? (y/n): ");
                        if (Console.ReadLine().ToLower() == "y")
                        {
                            try
                            {
                                repository.UpdateTask(existingTask);
                                Console.WriteLine("Изменения сохранены!");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
                                existingTask.Title = originalTitle;
                                existingTask.Deadline = originalDeadline;
                                existingTask.IsCompleted = originalStatus;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Изменения отменены!");
                            existingTask.Title = originalTitle;
                            existingTask.Deadline = originalDeadline;
                            existingTask.IsCompleted = originalStatus;
                        }
                        break;

                    case 4:
                        Console.WriteLine("Удаление задачи");

                        var dTasks = repository.GetAllTasks();
                        if (dTasks.Count == 0)
                        {
                            Console.WriteLine("Нет задач для удаления!");
                        }
                        foreach (var task in dTasks)
                        {
                            Console.WriteLine($"ID: {task.Id} | {task.Title}");
                        }
                        Console.WriteLine("Введите ID для удаления задачи: ");
                        if (int.TryParse(Console.ReadLine(), out int dTaskId))
                        {
                            try
                            {
                                repository.DeleteTask(dTaskId);
                                Console.WriteLine("Задача удалена");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Неверный формат ID");
                        }
                        break;

                    case 5:
                        isRunning = false;
                        break;

                    default:
                        Console.WriteLine("Ошибка, неверный номер опции");
                        break;
                }
            }
        }
    }
}