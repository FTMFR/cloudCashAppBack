using AutoMapper;
using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.Mapping
{
    /// <summary>
    /// پروفایل AutoMapper برای منو
    /// ============================================
    /// تبدیل بین Entity و DTO
    /// تاریخ‌ها در سطح JSON Serialization به شمسی تبدیل می‌شوند
    /// ============================================
    /// </summary>
    public class MenuProfile : Profile
    {
        public MenuProfile()
        {
            // ============================================
            // نگاشت tblMenu به MenuDto
            // فیلد IsMenu و ZamanInsert به صورت خودکار نگاشت می‌شوند
            // ============================================
            CreateMap<tblMenu, MenuDto>();
            CreateMap<MenuDto, tblMenu>();
        }
    }

    /// <summary>
    /// پروفایل AutoMapper برای کاربر
    /// ============================================
    /// تبدیل بین Entity و DTO
    /// تاریخ‌ها در سطح JSON Serialization به شمسی تبدیل می‌شوند
    /// ============================================
    /// </summary>
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // ============================================
            // نگاشت tblUser به UserDto
            // فیلدهای امنیتی و تاریخ‌ها به صورت خودکار نگاشت می‌شوند
            // ============================================
            CreateMap<tblUser, UserDto>();
            CreateMap<CreateUserDto, tblUser>();
            CreateMap<UpdateUserDto, tblUser>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }

    /// <summary>
    /// پروفایل AutoMapper برای گروه
    /// ============================================
    /// تبدیل بین Entity و DTO
    /// تاریخ‌ها در سطح JSON Serialization به شمسی تبدیل می‌شوند
    /// ============================================
    /// </summary>
    public class GrpProfile : Profile
    {
        public GrpProfile()
        {
            // ============================================
            // نگاشت tblGrp به GrpDto
            // فیلد ZamanInsert و ZamanLastEdit به صورت خودکار نگاشت می‌شوند
            // ============================================
            CreateMap<tblGrp, GrpDto>();
            CreateMap<CreateGrpDto, tblGrp>();
            CreateMap<UpdateGrpDto, tblGrp>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
