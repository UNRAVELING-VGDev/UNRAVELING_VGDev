using System;

public interface IBookPresenter
{
    event Action Opened;
    event Action Closed;
    event Action Stowed;
    void Draw();
    void Close();
    void Reopen();
    void Stow();
}
