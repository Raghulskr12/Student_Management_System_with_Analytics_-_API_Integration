using System.Collections.Generic;
using Core.Models;

namespace Core.Interfaces
{
    public interface IStudentRepository
    {
        void Add(Student student);
        IEnumerable<Student> GetAll();
        Student GetById(int id);
        void Update(Student student);
        void Delete(int id);
    }
}